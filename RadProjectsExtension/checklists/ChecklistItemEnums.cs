namespace net.adamec.dev.vs.extension.radprojects.checklists
{
    /// <summary>
    /// Check list item (step) type
    /// </summary>
    public enum ChecklistItemTypeEnum
    {
        /// <summary>
        /// Manual step
        /// </summary>
        Manual,
        /// <summary>
        /// Automated step - command run 
        /// </summary>
        Command
    }

    /// <summary>
    /// Check list item (step) status
    /// There should be only one Active, Running or Evaluate step for the whole check list
    /// </summary>
    public enum ChecklistItemStatusEnum
    {
        //----------------------------------------------
        //Pending (waiting for the execution)
        //----------------------------------------------
        /// <summary>
        /// Step is pending (waiting for the execution)
        /// </summary>
        Pending,

        //----------------------------------------------
        //"In Progress" step
        //----------------------------------------------    
        /// <summary>
        /// Step is active - to be executed
        /// </summary>
        Active,
        /// <summary>
        /// Automated step is running
        /// Currently not really used as I can't find a way how to track the progress of command run within the command line
        /// </summary>
        Running,
        /// <summary>
        /// Step has been executed and pending the evaluation
        /// For manual steps - the user executes the step in the evaluation status first!
        /// </summary>
        Evaluate,

        //----------------------------------------------
        //Terminal states
        //----------------------------------------------
        /// <summary>
        /// Step has been skipped
        /// </summary>
        Skipped,
        /// <summary>
        /// Step has finished as expected
        /// </summary>
        FinishedOk,
        /// <summary>
        /// Step has finished with error
        /// </summary>
        FinishedNok
    }
}
