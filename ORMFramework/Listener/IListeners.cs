using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework.Event;

namespace ORMFramework.Listener {
    public interface ISelectListener {
        void OnSelect ( SelectEvent @event, object sender );
    }

    public interface IDeleteListener {
        void OnDelete ( UpdateEvent @event, object sender );
    }

    public interface IUpdateListener {
        void OnUpdate ( UpdateEvent @event, object sender );
    }

    public interface IInsertListener {
        void OnInsert ( UpdateEvent @event, object sender );
    }

    public interface ISubmitListener {
        void OnSubmit ( ActionEvent @event, object sender );
    }

    public interface ICommandListener {
        void OnExecute ( CommandEvent @event, object sender );
    }
}
