namespace YazLab.Interaction
{
    public interface IOpenable
    {
        bool IsOpen { get; }
        void Open();
    }
}
