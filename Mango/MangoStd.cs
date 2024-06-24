using System;
using System.Windows.Input;
public class MangoStd
{
    [MangoFunction("print")]
    public static void Print(string str)
    {
        string[] split = str.Split('"');
        string value;
        if (!str.Contains('"'.ToString()))
        {
            if (!MangoInterpreter.variables.TryGetValue(split[0], out value))
            {
                MangoInterpreter.ReturnError(MangoInterpreter.ErrorType.InvalidVariable);
                return;
            }
        }
        else
        {
            value = split[1];
        }
        Console.WriteLine("[Mango] " + value);
    }

    [MangoFunction("sum")]
    public static string Sum(string str)
    {
        string[] values = str.Split(',');
        float n = 0;
        foreach (string s in values)
        {
            float tempN = 0;
            if (float.TryParse(s, out tempN))
            {
                n += tempN;
            }
            else
            {
                string realS = s;
                bool negative = s.Contains("-");
                if (negative)
                {
                    realS = s.Replace("-", "");
                }
                string value = "";
                if (MangoInterpreter.variables.TryGetValue(realS, out value))
                {

                    if (float.TryParse(value, out tempN))
                    {
                        n += tempN * (negative ? -1 : 1);
                    }
                }
                else
                {
                    return MangoInterpreter.ReturnError(MangoInterpreter.ErrorType.InvalidVariable);
                }
            }
        }
        return n.ToString();
    }
    [MangoFunction("mul")]
    public static string Mul(string str)
    {
        string[] values = str.Split(',');
        float n = 1;
        foreach (string s in values)
        {
            float tempN = 0;
            if (float.TryParse(s, out tempN))
            {
                n *= tempN;
            }
            else
            {
                string realS = s;
                bool negative = s.Contains("-");
                if (negative)
                {
                    realS = s.Replace("-", "");
                }
                string value = "";
                if (MangoInterpreter.variables.TryGetValue(realS, out value))
                {
                    if (float.TryParse(value, out tempN))
                    {
                        n *= tempN * (negative ? -1 : 1);
                    }
                }
                else
                {
                    return MangoInterpreter.ReturnError(MangoInterpreter.ErrorType.InvalidVariable);
                }
            }
        }
        return n.ToString();
    }

    [MangoFunction("div")]
    public static string Div(string str)
    {
        string[] values = str.Split(',');
        float n = 1;
        bool first = true;
        foreach (string s in values)
        {

            float tempN = 0;
            if (float.TryParse(s, out tempN))
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
                string realS = s;
                bool negative = s.Contains("-");
                if (negative)
                {
                    realS = s.Replace("-", "");
                }
                string value = "";
                if (MangoInterpreter.variables.TryGetValue(realS, out value))
                {
                    if (float.TryParse(value, out tempN))
                    {
                        if (first)
                        {
                            n = tempN * (negative ? -1 : 1);
                            first = false;
                        }
                        else
                        {
                            n /= tempN * (negative ? -1 : 1);
                        }
                    }
                }
                else
                {
                    return MangoInterpreter.ReturnError(MangoInterpreter.ErrorType.InvalidVariable);
                }
            }
        }
        return n.ToString();
    }


    public class MangoExtraFunctions
    {
        [MangoFunction("sqrt")]
        public static string Sqrt(string str)
        {
            float num;
            if (float.TryParse(str, out num))
            {
                if (num >= 0)
                {
                    return Math.Sqrt(num).ToString();
                }
                else
                {
                    return MangoInterpreter.ReturnError(MangoInterpreter.ErrorType.InvalidFunction);
                }
            }
            else
            {
                return MangoInterpreter.ReturnError(MangoInterpreter.ErrorType.InvalidVariable);
            }
        }

        [MangoFunction("max")]
        public static string Max(string str)
        {
            string[] values = str.Split(',');
            float max = float.MinValue;
            foreach (string s in values)
            {
                float num;
                if (float.TryParse(s, out num))
                {
                    if (num > max)
                    {
                        max = num;
                    }
                }
                else
                {
                    return MangoInterpreter.ReturnError(MangoInterpreter.ErrorType.InvalidVariable);
                }
            }
            return max.ToString();
        }

        [MangoFunction("abs")]
        public static string Abs(string str)
        {
            float num;
            if (float.TryParse(str, out num))
            {
                return Math.Abs(num).ToString();
            }
            else
            {
                return MangoInterpreter.ReturnError(MangoInterpreter.ErrorType.InvalidVariable);
            }
        }
    }
}



