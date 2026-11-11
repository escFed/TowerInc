public interface IProgressStore
{
    ProgressSaveData Load();
    void Save(ProgressSaveData progress);
}
