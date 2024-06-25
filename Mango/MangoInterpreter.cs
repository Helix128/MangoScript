using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

public class MangoFunction : Attribute
{
    public string name;
    public string info;
    public MangoFunction(string name)
    {
        this.name = name;
        this.info = "";
    }

public MangoFunction(string name, string info)
    {
        this.name = name;
        this.info = info;
    }
}

public class Function
{
    public string body;

    public void Execute()
    {
        MangoInterpreter.RunScript(body);
    }
}

public class Script
{
    public Dictionary<string, Function> functions;

    public Script()
    {
        functions = new Dictionary<string, Function>();
    }

    public void Execute(string function)
    {
        GetFunction(function)?.Execute();
    }

    public Function GetFunction(string name)
    {
        return functions.ContainsKey(name) ? functions[name] : null;
    }
}

public class MangoInterpreter
{
    public enum ErrorType
    {
        Unknown,
        InvalidVariable,
        InvalidFunction,
        InvalidArgs
    }

    public static string ReturnError(ErrorType errorType = ErrorType.Unknown)
    {
        return "[Mango] Error:" + errorType;
    }

    public static string PrintError(ErrorType errorType = ErrorType.Unknown)
    {
        string error = ReturnError(errorType);
        Console.WriteLine(error);
        return error;
    }

    static Dictionary<string, MethodInfo> actions;
    public static Dictionary<string, string> variables;
    static Stack<bool> ifStack;
    static bool executeLine;

    public static void Initialize()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        actions = new Dictionary<string, MethodInfo>();
        variables = new Dictionary<string, string>();
        Assembly assembly = Assembly.GetExecutingAssembly();
        var methods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(MangoFunction), false).Length > 0)
            .ToArray();
        foreach (MethodInfo method in methods)
        {
            string methodName = method.GetCustomAttribute<MangoFunction>().name;
            actions.Add(methodName, method);
        }
    }

    public static void SetVariable(string name, object value)
    {
        variables[name] = value.ToString();
    }

    public static string GetVariable(string name)
    {
        return variables.ContainsKey(name) ? variables[name] : null;
    }

    public static void ExecuteLine(string line)
    {
        if (line.Contains("//"))
        {
            return;
        }
        if (line.Contains("info"))
        {
            HandleInfo(line);
        }
        if (line.StartsWith("if"))
        {
            HandleIf(line);
        }
        else if (line.StartsWith("elif"))
        {
            HandleElif(line);
        }
        else if (line.StartsWith("else"))
        {
            HandleElse();
        }
        else if (line == "endif")
        {
            HandleEndif();
        }
        else if (line.StartsWith("for"))
        {
            HandleFor(line);
        }
        else if (line.StartsWith("while"))
        {
            HandleWhile(line);
        }
        else if (line.Contains("="))
        {
            if (executeLine) HandleAssignment(line);
        }
        else
        {
            if (executeLine) HandleFunction(line);
        }
    }
    public static void HandleInfo(string line)
    {
        string method = line.Split(':')[1];
        if (actions.Keys.Contains(method))
        {
            MethodInfo mInfo = actions[method];
            MangoFunction func = mInfo.GetCustomAttribute<MangoFunction>();
            Console.WriteLine("[Mango] "+func.name+": "+func.info);
        }
    }
    public static void HandleIf(string line)
    {
        string condition = line.Substring(2).Trim();
        bool conditionResult = EvaluateCondition(condition);
        ifStack.Push(conditionResult);
        executeLine = conditionResult;
    }

    public static void HandleElif(string line)
    {
        if (ifStack.Count == 0)
        {
            Console.WriteLine(ReturnError(ErrorType.InvalidFunction));
            return;
        }
        if (!ifStack.Peek())
        {
            string condition = line.Substring(4).Trim();
            bool conditionResult = EvaluateCondition(condition);
            ifStack.Pop();
            ifStack.Push(conditionResult);
            executeLine = conditionResult;
        }
        else
        {
            executeLine = false;
        }
    }

    public static void HandleElse()
    {
        if (ifStack.Count == 0)
        {
            Console.WriteLine(ReturnError(ErrorType.InvalidFunction));
            return;
        }
        if (!ifStack.Peek())
        {
            ifStack.Pop();
            ifStack.Push(true);
            executeLine = true;
        }
        else
        {
            executeLine = false;
        }
    }

    public static void HandleEndif()
    {
        if (ifStack.Count == 0)
        {
            Console.WriteLine(ReturnError(ErrorType.InvalidFunction));
            return;
        }
        ifStack.Pop();
        executeLine = ifStack.Count == 0 || ifStack.Peek();
    }

    public static void HandleAssignment(string line)
    {
        string[] splitVar;
        string varName;
        string varValue;

        if (line.Contains("+=") || line.Contains("-=") || line.Contains("*=") || line.Contains("/="))
        {
            string op = line.Contains("+=") ? "+=" : line.Contains("-=") ? "-=" : line.Contains("*=") ? "*=" : "/=";
            splitVar = line.Split(new string[] { op }, StringSplitOptions.None);
            varName = splitVar[0].Trim();
            varValue = varName + " " + op[0] + " " + splitVar[1].Trim();
        }
        else
        {
            splitVar = line.Split('=');
            varName = splitVar[0].Trim();
            varValue = splitVar[1].Trim();
        }

        variables[varName] = TryEvaluate(varValue);
    }

    public static string HandleFunction(string line)
    {
        int leftP = line.IndexOf('(');
        int rightP = line.IndexOf(')');
        if(leftP == -1 || rightP == -1) { return line; }
        string funcName = new string(line.Take(leftP).ToArray());
        string paramsFull = new string(line.Skip(leftP+1).Take(rightP - (leftP+1)).ToArray());

        string[] split = paramsFull.Split(",");
        foreach (string functionName in actions.Keys.Reverse())
        {
            if (funcName == functionName)
            {
               string result = (string)actions[functionName].Invoke(null, new object[] { split });
               return result;
            }
        }
        return line;
    }

    public static void RunScript(string code)
    {
        string[] sp = code.Split('\n');
        ifStack = new Stack<bool>();
        executeLine = true;

        foreach (string line in sp)
        {
            ExecuteLine(line.Trim());
        }
    }

    public static bool EvaluateCondition(string condition)
    {
        string[] split = condition.Split(new[] { "==", "!=", ">=", "<=", ">", "<" }, StringSplitOptions.None);
        if (split.Length != 2)
        {
            string value = TryEvaluate(split[0].Trim());
            bool result = false;
            bool.TryParse(value, out result);
            return result;
        }

        string left = TryEvaluate(split[0].Trim());
        string right = TryEvaluate(split[1].Trim());
        string op = condition.Substring(split[0].Length, condition.Length - split[0].Length - split[1].Length).Trim();
        switch (op)
        {
            case "==":
                return left == right;
            case "!=":
                return left != right;
            case ">=":
                return float.Parse(left) >= float.Parse(right);
            case "<=":
                return float.Parse(left) <= float.Parse(right);
            case ">":
                return float.Parse(left) > float.Parse(right);
            case "<":
                return float.Parse(left) < float.Parse(right);
            default:
                return false;
        }
    }
    public static void HandleFor(string line)
    {
        string[] parts = line.Substring(3).Trim().Split(' ');
        if (parts.Length != 5 || parts[1] != "=" || parts[3] != "to")
        {
            Console.WriteLine(ReturnError(ErrorType.InvalidFunction));
            return;
        }

        string varName = parts[0];
        int start = int.Parse(TryEvaluate(parts[2]));
        int end = int.Parse(TryEvaluate(parts[4]));

        for (int i = start; i <= end; i++)
        {
            SetVariable(varName, i);
            foreach (string subLine in parts[5].Split(';'))
            {
                ExecuteLine(subLine.Trim());
            }
        }
    }

    public static void HandleWhile(string line)
    {
        string condition = line.Substring(5).Trim();
        string[] parts = condition.Split(' ');
        while (EvaluateCondition(condition))
        {
            foreach (string subLine in parts[1].Split(';'))
            {
                ExecuteLine(subLine.Trim());
            }
        }
    }

    public static string TryEvaluate(string line)
    {
        string[] operators = { "+", "-", "*", "/" };
        foreach (string op in operators)
        {
            int opPos = line.IndexOf(op);
            if (opPos > 0)
            {
                string left = line.Substring(0, opPos).Trim();
                string right = line.Substring(opPos + 1).Trim();
                string leftVal = TryEvaluate(left);
                string rightVal = TryEvaluate(right);

                if (float.TryParse(leftVal, out float leftNum) && float.TryParse(rightVal, out float rightNum))
                {
                    switch (op)
                    {
                        case "+":
                            return (leftNum + rightNum).ToString();
                        case "-":
                            return (leftNum - rightNum).ToString();
                        case "*":
                            return (leftNum * rightNum).ToString();
                        case "/":
                            if (rightNum != 0)
                            {
                                return (leftNum / rightNum).ToString();
                            }
                            else
                            {
                                return ReturnError(ErrorType.InvalidVariable); // Division by zero error
                            }
                    }
                }
                else if (op == "+" && (!float.TryParse(leftVal, out _) || !float.TryParse(rightVal, out _)))
                {
                    return leftVal + rightVal;
                }
            }
        }

        foreach (string varName in variables.Keys.Reverse())
        {
            if (line == varName)
            {
                return variables[varName];
            }
        }
        return HandleFunction(line); 
    }

    public static Script LoadScript(string code)
    {
        Script script = new Script();
        string[] sp = code.Split('\n');
        string body = "";
        string name = "";
        bool readingFunction = false;
        Function func = null;

        foreach (string line in sp)
        {
            string[] splitSpc = line.Trim().Split(' ');
            if (readingFunction)
            {
                body += line + "\n";
            }
            if (splitSpc[0] == "function")
            {
                func = new Function();
                name = splitSpc[1];
                readingFunction = true;
            }
            else if (splitSpc[0] == "end")
            {
                readingFunction = false;
                func.body = body;
                script.functions.Add(name, func);
                body = "";
                name = "";
            }
        }
        return script;
    }
}
