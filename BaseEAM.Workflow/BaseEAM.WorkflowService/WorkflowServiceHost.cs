/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Workflows.Core.Infrastructure;
using Microsoft.Owin.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace BaseEAM.WorkflowService
{
    public class WorkflowServiceHost
    {
        private IDisposable _server = null;
        public void Start()
        {
            //config log4net
            string location = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(location);
            log4net.Config.XmlConfigurator.Configure(new FileInfo(folder + "\\App.config"));

            string baseAddress = "http://localhost:8989/";
            _server = WebApp.Start<WebApiStartup>(url: baseAddress);            
        }

        public void Stop()
        {
            if (_server != null)
            {
                _server.Dispose();
            }
            WorkflowServiceEngine.Instance.WorkflowContainer.Dispose();
        }
    }
}
