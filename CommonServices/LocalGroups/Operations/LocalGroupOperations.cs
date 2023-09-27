namespace SynchronizerLibrary.CommonServices.LocalGroups.Operations
{
    public class LocalGroupOperations
    {
        public AddLocalGroup addLocalGroup;
        public DeleteLocalGroup deleteLocalGroup;
        public ModifyLocalGroup modifyLocalGroup;

        public LocalGroupOperations()
        {
            addLocalGroup = new AddLocalGroup();
            deleteLocalGroup = new DeleteLocalGroup();
            modifyLocalGroup = new ModifyLocalGroup();
        }
    }
}
