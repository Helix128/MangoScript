using System;
using System.Windows.Input;

public class MangoStd
{
    [MangoFunction("print", info = "Prints text to the console.")]
    public static void Print(params string[] str)
    {
     
        string value = "";
        foreach (string s in str)
        {
            value += s+" ";
        }
        Console.WriteLine("[Mango] " + value);
    }

    [MangoFunction("sum")]
    public static string Sum(string[] str)
    {
        float n = 0;
        foreach (string s in str)
        {
            
            string tempS = MangoInterpreter.TryEvaluate(s);
            float tempN;
            if (float.TryParse(tempS, out tempN))
            {
                n += tempN;
            }
            else
            {
                return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidVariable);
            }
        }
        return n.ToString();
    }

    [MangoFunction("mul")]
    public static string Mul(string[] str)
    {
        float n = 1;
        foreach (string s in str)
        {
            string tempS = MangoInterpreter.TryEvaluate(s);
            float tempN;
            if (float.TryParse(tempS, out tempN))
            {
                n *= tempN;
            }
            else
            {
                return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidVariable);
            }
        }
        return n.ToString();
    }

    [MangoFunction("div")]
    public static string Div(string[] str)
    {
        float n = 1;
        bool first = true;
        foreach (string s in str)
        {
            string tempS = MangoInterpreter.TryEvaluate(s);
            float tempN;
            if (float.TryParse(tempS, out tempN))
            {
                if (first)
                {
                    n = tempN;
                    first = false;
                }
                else
                {
                    n /= tempN;
                }
            }
            else
            {
                return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidVariable);
            }
        }
        return n.ToString();
    }

    public class MangoExtraFunctions
    {
        [MangoFunction("sqrt")]
        public static string Sqrt(string[] str)
        {
            if (str.Length != 1) return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidFunction);

            string tempS = MangoInterpreter.TryEvaluate(str[0]);
            float num;
            if (float.TryParse(tempS, out num))
            {
                if (num >= 0)
                {
                    return Math.Sqrt(num).ToString();
                }
                else
                {
                    return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidFunction);
                }
            }
            else
            {
                return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidVariable);
            }
        }

        [MangoFunction("max")]
        public static string Max(string[] str)
        {
            float max = float.MinValue;
            foreach (string s in str)
            {
                string tempS = MangoInterpreter.TryEvaluate(s);
                float num;
                if (float.TryParse(tempS, out num))
                {
                    if (num > max)
                    {
                        max = num;
                    }
                }
                else
                {
                    return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidVariable);
                }
            }
            return max.ToString();
        }

        [MangoFunction("min")]
        public static string Min(string[] str)
        {
            float min = float.MaxValue;
            foreach (string s in str)
            {
                string tempS = MangoInterpreter.TryEvaluate(s);
                float num;
                if (float.TryParse(tempS, out num))
                {
                    if (num < min)
                    {
                        min = num;
                    }
                }
                else
                {
                    return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidVariable);
                }
            }
            return min.ToString();
        }

        [MangoFunction("abs")]
        public static string Abs(string[] str)
        {
            if (str.Length != 1) return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidFunction);

            string tempS = MangoInterpreter.TryEvaluate(str[0]);
            float num;
            if (float.TryParse(tempS, out num))
            {
                return Math.Abs(num).ToString();
            }
            else
            {
                return MangoInterpreter.PrintError(MangoInterpreter.ErrorType.InvalidVariable);
            }
        }
    }
}
