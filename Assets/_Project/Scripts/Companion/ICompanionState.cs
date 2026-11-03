namespace YazLab.Companion
{
    public interface ICompanionState
    {
        string Name { get; }
        void Enter(CompanionController companion);
        void Tick(CompanionController companion);
        void Exit(CompanionController companion);
    }
}
