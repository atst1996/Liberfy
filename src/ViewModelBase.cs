﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WpfMvvmToolkit
{
    public class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        public ViewModelBase()
        {
            this._registeredCommands = new Collection<ICommand>();
        }

        private readonly ICollection<ICommand> _registeredCommands;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsDisposed { get; private set; }

        protected bool SetProperty<T>(ref T refValue, T newValue, [CallerMemberName] string propertyName = "")
        {
            if (!object.Equals(refValue, newValue))
            {
                refValue = newValue;
                this.RaisePropertyChanged(propertyName);

                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool SetProperty<T>(ref T refValue, T newValue, Command command, [CallerMemberName] string propertyName = "")
        {
            if (this.SetProperty(ref refValue, newValue, propertyName))
            {
                command?.RaiseCanExecute();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool SetProperty<T>(ref T refValue, T newValue, Command<T> command, [CallerMemberName] string propertyName = "")
        {
            if (this.SetProperty(ref refValue, newValue, propertyName))
            {
                command?.RaiseCanExecute();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void SetPropertyForce<T>(ref T refValue, T newValue, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            refValue = newValue;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void RaisePropertiesChanged(params string[] propertyNames)
        {
            if (this.PropertyChanged != null)
            {
                foreach (var name in propertyNames)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public Command RegisterCommand(Command command)
        {
            this._registeredCommands.Add(command);
            return command;
        }

        public Command<T> RegisterCommand<T>(Command<T> command)
        {
            this._registeredCommands.Add(command);
            return command;
        }

        internal virtual void OnInitialized() { }

        internal virtual bool CanClose() => true;

        internal virtual void OnClosed() { }

        public virtual void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;

            foreach (var command in this._registeredCommands)
            {
                if (command is IDisposable disposableCommand)
                {
                    disposableCommand.Dispose();
                }
            }
            this._registeredCommands.Clear();
        }
    }
}
