﻿using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using log4net.Config;
using System.IO;

namespace ORMFramework.LogHelper {
    public class LogHelper {
        private static ILog _log;

        public static void InitLog ( string configFilePath ) {
            if ( string.IsNullOrEmpty ( configFilePath ) ) {
                configFilePath = Environment.CurrentDirectory + "\\Config.xml";
            }
            XmlConfigurator.Configure ( new FileInfo ( configFilePath ) );
            _log = LogManager.GetLogger ( "DefaultLogger" );
        }

        public static void Debug ( string message ) {
            _log.Debug ( message );
        }

        public static void Error ( string message ) {
            _log.Error ( message );
        }

        public static void Fatal ( string message ) {
            _log.Fatal ( message );
        }

        public static void Warn ( string message ) {
            _log.Warn ( message );
        }

        public static void Info ( string message ) {
            _log.Info ( message );
        }
    }
}
