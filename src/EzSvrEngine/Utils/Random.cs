namespace EzSvrEngine.Utils
{
    public static class Random
    {
        private static readonly Random getrandom = new Random();

        public static int RandInt32()
        {
            lock (getrandom) // synchronize
            {
                return getrandom.Next(Int32.MinValue, Int32.MaxValue);
            }
        }

        public static int RandUInt32(){
            lock (getrandom) // synchronize
            {
                return getrandom.Next(UInt32.MinValue, UInt32.MaxValue);
            }
        }
    }
}
