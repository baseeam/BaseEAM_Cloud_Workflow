/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Core.Data;
using BaseEAM.Core.Domain;
using BaseEAM.Workflows.Activities.Design;
using BaseEAM.Workflows.Core.Infrastructure;
using System.Activities;
using System.ComponentModel;
using System.Linq;

namespace BaseEAM.Workflows.Activities
{
    [Designer(typeof(WaitForActionDesigner))]
    public sealed class WaitForAction<TResult> : NativeActivity<TResult>
    {
        public WaitForAction()
            : base()
        {
        }

        [RequiredArgument]
        public InArgument<WorkflowInput> WorkflowInput { get; set; }

        public string AvailableActions { get; set; }

        protected override bool CanInduceIdle
        {
            //override when the custom activity is allowed to make the workflow go idle
            get
            {
                return true;
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark("ActionExecuted", new BookmarkCallback(this.ReceivedEvent));
            //persist AvailableActions to be get in the UI
            var workflowInput = this.WorkflowInput.Get(context);
            var assignmentRepository = WorkflowServiceEngine.Instance.WorkflowContainerManager.Resolve<IRepository<Assignment>>();
            var assignment = assignmentRepository.GetAll()
                .Single(a => a.EntityId == workflowInput.EntityId && a.EntityType == workflowInput.EntityType
                            && a.WorkflowDefinitionId == workflowInput.WorkflowDefinitionId);
            assignment.AvailableActions = this.AvailableActions;
            assignmentRepository.UpdateAndCommit(assignment);
        }

        void ReceivedEvent(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            this.Result.Set(context, (TResult)obj);
        }
    }
}
