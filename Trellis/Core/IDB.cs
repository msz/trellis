namespace Trellis.Core
{
    public interface IDB
    {
        IDBCollection GetCollection(string name);
    }
}
