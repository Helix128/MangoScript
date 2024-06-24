using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

public class MangoFunction : Attribute
{
    public string name;

    public MangoFunction(string name)
    {
        this.name = name; 
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
        GetFunction(function).Execute();
    }
    public Function GetFunction(string name)
    {
        if (functions.ContainsKey(name))
        {
            return functions[name];
        }
        else
        {
            return null;
        }
    }
}

public class MangoInterpreter 
{
    public enum ErrorType
    {
        Unknown,
        InvalidVariable,
        InvalidFunction
    }

    public static string ReturnError(ErrorType errorType = ErrorType.Unknown)
    {
        return "[Mango] Error:" + errorType;
    }

    static Dictionary<string, MethodInfo> actions;
    public static Dictionary<string, string> variables;
    static Stack<bool> ifStack;
    static bool executeLine;

    public static void Initialize()
    {
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
        if (variables.ContainsKey(name))
        {
            variables[name] = value.ToString();
        }
        else
        {
            variables.Add(name, value.ToString());
        }
    }
    public static string GetVariable(string name)
    {
        if (variables.ContainsKey(name))
        {
            return variables[name];
        }
        else
        {
            return null;
        }
    }
    public static void ExecuteLine(string line)
    {
        if (line.Contains("//"))
        {
            return;
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
        else if (line.Contains("="))
        {
            if (executeLine) HandleAssignment(line);
        }
        else
        {
            if (executeLine) HandleFunction(line);
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
        string[] splitVar = line.Split('=');
        string varName = splitVar[0].Trim();
        string varValue = splitVar[1].Trim();
        if (!variables.ContainsKey(varName))
        {
            variables.Add(varName, TryEvaluate(varValue));
        }
        else
        {
            variables[varName] = TryEvaluate(varValue);
        }
    }

    public static void HandleFunction(string line)
    {
        string[] split = line.Split(':');
        foreach (string functionName in actions.Keys.Reverse())
        {
            if (split[0] == functionName)
            {
                actions[functionName].Invoke(null, new object[] { split[1] });
            }
        }
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

    public static string TryEvaluate(string line)
    {
        string[] split = line.Split(':');
        string result = null; 
        foreach (string varName in variables.Keys.Reverse())
        {
            if (split[0] == varName)
            {
                return variables[varName];
            }
        }
        foreach (string functionName in actions.Keys.Reverse())
        {
            if (split[0] == functionName)
            {
                return actions[functionName].Invoke(null, new object[] { split[1] }).ToString();
            }
        }
        if (result == null)
        {
            return line;
        }
        return result;
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
