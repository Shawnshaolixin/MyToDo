using MyToDo.Api.Entities.Workflow;

namespace MyToDo.Api.Services.Workflow
{
    public interface IWorkflowBookmarkService
    {
        /// <summary>
        /// Adds a new active bookmark to the EF change tracker. Does not call SaveChanges.
        /// </summary>
        WorkflowBookmark Create(Guid workflowInstanceId, Guid executionTokenId, Guid workflowNodeInstanceId,
            string bookmarkType, string bookmarkKey);

        /// <summary>
        /// Returns the first active bookmark matching the given type and key, or null.
        /// </summary>
        Task<WorkflowBookmark?> FindAsync(string bookmarkType, string bookmarkKey, CancellationToken cancellationToken);

        /// <summary>
        /// Marks a bookmark as consumed. Does not call SaveChanges.
        /// </summary>
        void Consume(WorkflowBookmark bookmark);
    }
}
