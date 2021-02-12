/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using Autofac;
using BaseEAM.Core.Infrastructure.DependencyManagement;

namespace BaseEAM.Workflows.Core.Infrastructure
{
    public class WorkflowServiceEngine
    {
        private static readonly WorkflowServiceEngine instance = new WorkflowServiceEngine();

        public ContainerManager WorkflowContainerManager { get; set; }
        public ContainerBuilder WorkflowBuilder { get; set; }
        public IContainer WorkflowContainer { get; set; }

        private WorkflowServiceEngine()
        {
            WorkflowBuilder = new ContainerBuilder();
        }

        public static WorkflowServiceEngine Instance { get { return instance; } }

        public void Initialize()
        {
        }

        public void Build()
        {
            WorkflowContainer = WorkflowBuilder.Build();
            WorkflowContainerManager = new ContainerManager(WorkflowContainer);
        }
    }
}
