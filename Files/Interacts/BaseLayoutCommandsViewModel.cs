using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace Files.Interacts
{
    public class BaseLayoutCommandsViewModel
    {
        #region Private Members

        private readonly IBaseLayoutCommandImplementationModel commandsModel;

        #endregion

        #region Constructor

        public BaseLayoutCommandsViewModel(IBaseLayoutCommandImplementationModel commandsModel)
        {
            this.commandsModel = commandsModel;

            InitializeCommands();
        }

        #endregion

        #region Command Initialization

        private void InitializeCommands()
        {
            ExampleCommand = new RelayCommand<EventArgs>(commandsModel.Example);
        }

        #endregion

        #region Commands

        // TODO: We'll have there all BaseLayout commands to bind to -> and these will call implementation in commandsModel

        public ICommand ExampleCommand { get; private set; }

        #endregion
    }
}
