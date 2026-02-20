public interface ILevelUpTextFormatter<T>
{
    string BuildDescription(T data);
}