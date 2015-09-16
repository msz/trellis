namespace Trellis.Core
{
    internal class ArrayModifyEvent
    {
        public int Index { get;set; }
        public object Value { get;set; }
        public ArrayModifyType Type { get;set; }
        public ArrayModifyEvent(ArrayModifyType type, object value, int index = 0)
        {
            Index = index;
            Value = value;
            Type = type;
        }
    }
}