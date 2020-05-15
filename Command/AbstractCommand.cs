using System;
using Anvil.CSharp.Core;

namespace Anvil.CSharp.Command
{
    /// <summary>
    /// Concrete implementation of <see cref="ICommand{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of <see cref="ICommand"/> to use.</typeparam>
    public abstract class AbstractCommand<T> : AbstractCommand, ICommand<T>
        where T : class, ICommand
    {
        /// <summary>
        /// <inheritdoc cref="ICommand{T}.OnComplete"/>
        /// </summary>
        public new event Action<T> OnComplete;

        /// <summary>
        /// <inheritdoc cref="ICommand{T}.OnDisposing"/>
        /// </summary>
        public new event Action<T> OnDisposing;

        protected override void DisposeSelf()
        {
            OnComplete = null;
            OnDisposing = null;
            base.DisposeSelf();
        }

        protected override void DispatchOnComplete()
        {
            OnComplete?.Invoke(this as T);
        }

        protected override void DispatchOnDisposing()
        {
            OnDisposing?.Invoke(this as T);
        }
    }

    /// <summary>
    /// Concrete implementation of <see cref="ICommand"/>
    /// </summary>
    public abstract class AbstractCommand : AbstractAnvilDisposable, ICommand
    {
        /// <summary>
        /// <inheritdoc cref="ICommand.OnComplete"/>
        /// </summary>
        public event Action<ICommand> OnComplete;

        /// <summary>
        /// <inheritdoc cref="ICommand.OnDisposing"/>
        /// </summary>
        public event Action<ICommand> OnDisposing;

        /// <summary>
        /// <inheritdoc cref="ICommand.State"/>
        /// </summary>
        public CommandState State { get; private set; } = CommandState.Initialized;

        protected AbstractCommand()
        {
        }

        protected override void DisposeSelf()
        {
            OnComplete = null;
            DispatchOnDisposing();

            base.DisposeSelf();
        }

        /// <summary>
        /// <inheritdoc cref="ICommand.Execute"/>
        /// </summary>
        /// <exception cref="Exception">Occurs when the <see cref="State"/> is not CommandState.Initialized</exception>
        public void Execute()
        {
            if (State != CommandState.Initialized)
            {
                throw new Exception($"Tired to call {nameof(Execute)} on {this} but State was {State} instead of {CommandState.Initialized}!");
            }

            State = CommandState.Executing;
            ExecuteCommand();
        }

        /// <summary>
        /// Called from <see cref="AbstractCommand.Execute"/>. Override and implement to execute the logic of the command.
        /// </summary>
        protected abstract void ExecuteCommand();

        /// <summary>
        /// Call when the command is complete. Command will auto dispose after dispatching <see cref="OnComplete"/>
        /// </summary>
        /// <exception cref="Exception">Occurs when <see cref="State"/> is not CommandState.Executing</exception>
        protected void CompleteCommand()
        {
            if (State != CommandState.Executing)
            {
                throw new Exception($"Tried to call {nameof(CompleteCommand)} on {this} but State was {State} instead of {CommandState.Executing}!");
            }
            State = CommandState.Completed;
            DispatchOnComplete();
            Dispose();
        }

        protected virtual void DispatchOnComplete()
        {
            OnComplete?.Invoke(this);
        }

        protected virtual void DispatchOnDisposing()
        {
            OnDisposing?.Invoke(this);
        }
    }
}

