/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Core;
using BaseEAM.Core.Data;
using BaseEAM.Core.Domain;
using BaseEAM.Workflows.Activities.Design;
using BaseEAM.Workflows.Core.Infrastructure;
using System.Activities;
using System.ComponentModel;

namespace BaseEAM.Workflows.Activities
{
    [Designer(typeof(ActionDesigner))]
    public sealed class Action : CodeActivity
    {
        [RequiredArgument]
        [Category("Arguments")]
        [Description("The input data when starting a new workflow instance.")]
        public InArgument<WorkflowInput> WorkflowInput { get; set; }

        [RequiredArgument]
        [Category("Arguments")]
        [Description("The variable data when running a workflow instance.")]
        public InArgument<WorkflowVariable> WorkflowVariable { get; set; }

        [Category("Action")]
        [DefaultValue("")]
        [Description("The action will be executed. It's a method signature, such as: IWorkOrderService.CloseWork. The param of this method is always one: entityId.")]
        public string ActionName { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var workflowInput = this.WorkflowInput.Get(context);
            var workflowVariable = this.WorkflowVariable.Get(context);
            //set the currentUser to ThreadStatic to be used later
            var userRepository = WorkflowServiceEngine.Instance.WorkflowContainerManager.Resolve<IRepository<User>>();
            var currentUser = userRepository.GetById(workflowVariable.CurrentUserId);
            WorkflowContext.CurrentUser = currentUser;

            ActivityHelper.ExecuteAction(this.ActionName, workflowVariable.EntityId);
        }
    }
}
