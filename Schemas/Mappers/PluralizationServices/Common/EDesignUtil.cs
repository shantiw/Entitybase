namespace System.Data.Entity.Design.Common
{
    internal static class EDesignUtil
    {
        static internal T CheckArgumentNull<T>(T value, string parameterName) where T : class
        {
            if (null == value)
            {
                throw ArgumentNull(parameterName);
            }
            return value;
        }

        static internal ArgumentNullException ArgumentNull(string parameter)
        {
            ArgumentNullException e = new(parameter);
            return e;
        }
    }
}
