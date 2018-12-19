namespace CVM
{
    public class Tuple2<T1,T2>
    {
        public T1 Item1;
        public T2 Item2;
        public Tuple2(T1 a,T2 b)
        {
            Item1 = a;
            Item2 = b;
        }
    }
    public class Tuple2<T1, T2,T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public Tuple2(T1 a, T2 b,T3 c)
        {
            Item1 = a;
            Item2 = b;
            Item3 = c;
        }
    }
    public class Tuple2<T1, T2, T3,T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;

        public Tuple2(T1 a, T2 b, T3 c,T4 d)
        {
            Item1 = a;
            Item2 = b;
            Item3 = c;
            Item4 = d;
        }
    }
}
