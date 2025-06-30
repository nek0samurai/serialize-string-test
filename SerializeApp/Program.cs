using System;
using System.Linq;
using System.Numerics;
using System.Text;


class Program
{
    static void Main()
    {
        int[] testArr1 = { 1, 3, 5, 7, 1, 2, 3, 4 };
        int[] testArr2 = MakeRandom(50);
        int[] testArr3 = MakeRandom(100);
        int[] testArr4 = MakeRandom(500);
        int[] testArr5 = MakeRandom(1000);
        int[] testArr6 = Enumerable.Range(1, 9).ToArray();
        int[] testArr7 = Enumerable.Range(10, 90).ToArray();
        int[] testArr8 = Enumerable.Range(100, 900).ToArray();
        Console.WriteLine(string.Join(",", testArr7));
        Test(testArr8);
    }

    static int[] MakeRandom(int value)
    {
        int[] arr = new int[value];
        Random rand = new();
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = rand.Next(1, 301);
        }
        return arr;
    }

    static void Test(int[] input)
    {
        string enc = CompactSerializer.Serialize(input);
        var dec = CompactSerializer.Deserialize(enc);
        bool ok = dec.OrderBy(x => x).SequenceEqual(input.OrderBy(x => x));
        float ratio = input.Length / enc.Length;
        //Console.WriteLine($"{input.Length} items: enc={enc.Length}, ratio={ratio:F2}, OK={ok}");
        Console.WriteLine(string.Join(",", dec));
        Console.WriteLine(enc);
        Console.WriteLine("длина исходника: " + dec.Length + "\nдлина сериализованного: " + enc.Length + "\nкоеф сжатия: " + ratio);

    }
}
public static class CompactSerializer
{
    
    private const string Base91Dict = " !#$%&()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}";

    private static string CompressBase91(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        // 1) Unique chars in input
        var unique = input.Distinct().ToList();
        if (unique.Count == 1)
        {
            unique.Insert(0, '~'); // ensure at least two
        }
        // avoid leading-zero loss
        if (input[0] == unique[0])
        {
            var first = unique[0];
            unique.RemoveAt(0);
            unique.Add(first);
        }
        var charToValue = unique.Select((c, i) => new { c, i }).ToDictionary(x => x.c, x => x.i);
        int customBase = unique.Count;

        // 2) Convert input string (baseX) to BigInteger
        BigInteger value = BigInteger.Zero;
        foreach (char c in input)
        {
            value = value * customBase + charToValue[c];
        }

        // 3) Convert BigInteger to Base91
        BigInteger b91Base = Base91Dict.Length;
        var sb = new StringBuilder();
        while (value > 0)
        {
            var rem = (int)(value % b91Base);
            sb.Append(Base91Dict[rem]);
            value /= b91Base;
        }
        var encoded = new string(sb.ToString().Reverse().ToArray());

        // Prefix custom alphabet and separator '~'
        var dictStr = new string(unique.ToArray());
        return dictStr + '~' + encoded;
    }

    private static string DecompressBase91(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        int sep = input.LastIndexOf('~');
        var dictStr = input.Substring(0, sep);
        var encoded = input.Substring(sep + 1);
        var customBase = dictStr.Length;
        var valueToChar = dictStr.Select((c, i) => new { c, i }).ToDictionary(x => x.i, x => x.c);

        BigInteger value = BigInteger.Zero;
        foreach (char c in encoded)
        {
            int idx = Base91Dict.IndexOf(c);
            value = value * Base91Dict.Length + idx;
        }

        var stack = new Stack<char>();
        while (value > 0)
        {
            var rem = (int)(value % customBase);
            stack.Push(valueToChar[rem]);
            value /= customBase;
        }
        return new string(stack.ToArray());
    }
    
    // Сериализация
    public static string Serialize(int[] data)
    {
        if (data == null || data.Length == 0) return string.Empty;
        
        var arr = data.OrderBy(x => x).ToArray();
        var deltas = new int[arr.Length];
        deltas[0] = arr[0];
        for (int i = 1; i < arr.Length; i++)
            deltas[i] = arr[i] - arr[i - 1];
        
        var sb = new StringBuilder();
        foreach (var d in deltas)
        {
            var s = d.ToString();
            sb.Append(new string('~', s.Length - 1));
            sb.Append(s);
        }
        var deltaStr = sb.ToString();
        
        return CompressBase91(deltaStr);

    }

    // Десериализация
    public static int[] Deserialize(string s)
    {
        if (string.IsNullOrEmpty(s)) return Array.Empty<int>();
        // 1) Decompress Base91 to delta-string
        var deltaStr = DecompressBase91(s);
        // 2) Parse deltas
        var deltas = new List<int>();
        for (int i = 0; i < deltaStr.Length;)
        {
            int tildes = 0;
            while (i < deltaStr.Length && deltaStr[i] == '~') { tildes++; i++; }
            int len = tildes + 1;
            var num = deltaStr.Substring(i, len);
            deltas.Add(int.Parse(num));
            i += len;
        }
        // 3) Reconstruct original numbers
        var result = new int[deltas.Count];
        if (deltas.Count > 0) result[0] = deltas[0];
        for (int i = 1; i < deltas.Count; i++)
            result[i] = result[i - 1] + deltas[i];
        return result;
    }
}

