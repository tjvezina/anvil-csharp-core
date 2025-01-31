﻿using System;
using System.Diagnostics;
using Anvil.CSharp.Core;

namespace Anvil.CSharp.Content
{
    /// <summary>
    /// A logical group of <see cref="AbstractContentController"/>/<see cref="IContent"/> pairs to be shown.
    /// Many <see cref="AbstractContentGroup"/> can be added to the controlling <see cref="AbstractContentManager"/>.
    /// </summary>
    public abstract class AbstractContentGroup: AbstractAnvilBase
    {
        /// <summary>
        /// <inheritdoc cref="AbstractContentController.OnLoadStart"/>
        /// </summary>
        public event Action<AbstractContentController> OnLoadStart;
        /// <summary>
        /// <inheritdoc cref="AbstractContentController.OnLoadComplete"/>
        /// </summary>
        public event Action<AbstractContentController> OnLoadComplete;
        /// <summary>
        /// <inheritdoc cref="AbstractContentController.OnPlayInStart"/>
        /// </summary>
        public event Action<AbstractContentController> OnPlayInStart;
        /// <summary>
        /// <inheritdoc cref="AbstractContentController.OnPlayInComplete"/>
        /// </summary>
        public event Action<AbstractContentController> OnPlayInComplete;
        /// <summary>
        /// <inheritdoc cref="AbstractContentController.OnPlayOutStart"/>
        /// </summary>
        public event Action<AbstractContentController> OnPlayOutStart;
        /// <summary>
        /// <inheritdoc cref="AbstractContentController.OnPlayOutComplete"/>
        /// </summary>
        public event Action<AbstractContentController> OnPlayOutComplete;

        /// <summary>
        /// The <see cref="ContentGroupConfigVO"/> for configuring the <see cref="AbstractContentGroup"/>
        /// </summary>
        public readonly ContentGroupConfigVO ConfigVO;

        /// <summary>
        /// The controlling <see cref="AbstractContentManager"/>
        /// </summary>
        public readonly AbstractContentManager ContentManager;

        /// <summary>
        /// The active <see cref="AbstractContentController"/> currently being shown.
        /// </summary>
        public AbstractContentController ActiveContentController { get; private set; }

        private AbstractContentController m_PendingContentController;

        protected AbstractContentGroup(AbstractContentManager contentManager, ContentGroupConfigVO configVO)
        {
            ContentManager = contentManager;
            ConfigVO = configVO;
        }

        protected override void DisposeSelf()
        {
            OnLoadStart = null;
            OnLoadComplete = null;
            OnPlayInStart = null;
            OnPlayInComplete = null;
            OnPlayOutStart = null;
            OnPlayOutComplete = null;

            ActiveContentController?.Dispose();
            ActiveContentController = null;

            m_PendingContentController?.Dispose();
            m_PendingContentController = null;

            base.DisposeSelf();
        }

        /// <summary>
        /// Shows an <see cref="AbstractContentController"/> in this group.
        /// </summary>
        /// <param name="contentController">The instance of <see cref="AbstractContentController"/> to be shown.</param>
        public void Show(AbstractContentController contentController)
        {
            //TODO: Validate the passed in controller to ensure we avoid weird cases - https://github.com/scratch-games/anvil-unity-core/issues/3

            RemoveLifeCycleListeners(m_PendingContentController);
            m_PendingContentController = contentController;
            m_PendingContentController.ContentGroup = this;
            AttachLifeCycleListeners(m_PendingContentController);

            //If there's an Active Controller currently being shown, we need to clear it.
            if (ActiveContentController != null)
            {
                ActiveContentController.InternalPlayOut();
            }
            else
            {
                //TODO: Should we wait one frame here via UpdateHandle? - https://github.com/scratch-games/anvil-csharp-core/issues/20
                //Otherwise we can just show the pending controller
                ShowPendingContentController();
            }
        }

        /// <summary>
        /// Clears this group so that no <see cref="AbstractContentController"/>/<see cref="IContent"/> pair is being shown.
        /// </summary>
        public void Clear()
        {
            Show(null);
        }

        private void ShowPendingContentController()
        {
            //If there's nothing to show, early return. Will occur on a Clear.
            if (m_PendingContentController == null)
            {
                return;
            }
            //We can't show the pending controller right away because we may not have the necessary assets loaded.
            //So we need to construct a Sequential Command and populate with the required commands to load the assets needed.
            //TODO: Handle loading - https://github.com/scratch-games/anvil-unity-core/issues/2

            ActiveContentController = m_PendingContentController;
            m_PendingContentController = null;

            ActiveContentController.InternalLoad();
        }

        private void ContentController_OnLoadStart(AbstractContentController contentController)
        {
            Debug.Assert(contentController == ActiveContentController,
                $"Controller {contentController} dispatched OnLoadStart but it is not the same as the {nameof(ActiveContentController)} which is {ActiveContentController}!");

            OnLoadStart?.Invoke(ActiveContentController);
        }

        private void ContentController_OnLoadComplete(AbstractContentController contentController)
        {
            Debug.Assert(contentController == ActiveContentController,
                $"Controller {contentController} dispatched OnLoadComplete but it is not the same as the {nameof(ActiveContentController)} which is {ActiveContentController}!");


            OnLoadComplete?.Invoke(ActiveContentController);

            ActiveContentController.InternalInitAfterLoadComplete();
            ActiveContentController.InternalPlayIn();
        }

        private void ContentController_OnPlayInStart(AbstractContentController contentController)
        {
            Debug.Assert(contentController == ActiveContentController,
                $"Controller {contentController} dispatched OnPlayInStart but it is not the same as the {nameof(ActiveContentController)} which is {ActiveContentController}!");


            OnPlayInStart?.Invoke(ActiveContentController);
        }

        private void ContentController_OnPlayInComplete(AbstractContentController contentController)
        {
            Debug.Assert(contentController == ActiveContentController,
                $"Controller {contentController} dispatched OnPlayInComplete but it is not the same as the {nameof(ActiveContentController)} which is {ActiveContentController}!");


            OnPlayInComplete?.Invoke(ActiveContentController);

            ActiveContentController.InternalInitAfterPlayInComplete();
        }

        private void ContentController_OnPlayOutStart(AbstractContentController contentController)
        {
            Debug.Assert(contentController == ActiveContentController,
                $"Controller {contentController} dispatched OnPlayOutStart but it is not the same as the {nameof(ActiveContentController)} which is {ActiveContentController}!");


            OnPlayOutStart?.Invoke(ActiveContentController);
        }

        private void ContentController_OnPlayOutComplete(AbstractContentController contentController)
        {
            Debug.Assert(contentController == ActiveContentController,
                $"Controller {contentController} dispatched OnPlayOutComplete but it is not the same as the {nameof(ActiveContentController)} which is {ActiveContentController}!");

            OnPlayOutComplete?.Invoke(ActiveContentController);
            RemoveLifeCycleListeners(ActiveContentController);
            ActiveContentController?.Dispose();
            ActiveContentController = null;

            ShowPendingContentController();
        }

        private void AttachLifeCycleListeners(AbstractContentController contentController)
        {
            if (contentController == null)
            {
                return;
            }

            contentController.OnLoadStart += ContentController_OnLoadStart;
            contentController.OnLoadComplete += ContentController_OnLoadComplete;
            contentController.OnPlayInStart += ContentController_OnPlayInStart;
            contentController.OnPlayInComplete += ContentController_OnPlayInComplete;
            contentController.OnPlayOutStart += ContentController_OnPlayOutStart;
            contentController.OnPlayOutComplete += ContentController_OnPlayOutComplete;
        }

        private void RemoveLifeCycleListeners(AbstractContentController contentController)
        {
            if (contentController == null)
            {
                return;
            }

            contentController.OnLoadStart -= ContentController_OnLoadStart;
            contentController.OnLoadComplete -= ContentController_OnLoadComplete;
            contentController.OnPlayInStart -= ContentController_OnPlayInStart;
            contentController.OnPlayInComplete -= ContentController_OnPlayInComplete;
            contentController.OnPlayOutStart -= ContentController_OnPlayOutStart;
            contentController.OnPlayOutComplete -= ContentController_OnPlayOutComplete;
        }
    }
}

