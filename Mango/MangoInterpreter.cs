
using System.Reflection;
using System.Text;

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

    public async Task Execute()
    {
        await MangoInterpreter.RunScript(body);
    }
}

public class Script
{
    public Dictionary<string, Function> functions;

    public Script()
    {
        functions = new Dictionary<string, Function>();
    }

    public async Task Execute(string function)
    {
        await GetFunction(function)?.Execute();
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
        return "Error:" + errorType;
    }

    public static string PrintError(ErrorType errorType = ErrorType.Unknown)
    {
        string error = ReturnError(errorType);
        Console.WriteLine("[Mango] " + error);
        return error;
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
        variables[name] = value.ToString();
    }

    public static string GetVariable(string name)
    {
        return variables.ContainsKey(name) ? variables[name] : null;
    }

    public static async Task ExecuteLine(string line)
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
        else if (line.Contains("=")&&line.Contains("(")==false)
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
            Console.WriteLine("[Mango] " + func.name + ": " + func.info);
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
        int rightP = line.LastIndexOf(')');
        if (leftP == -1 || rightP == -1) { return line; }
        string funcName = new string(line.Take(leftP).ToArray());
        string paramsFull = new string(line.Skip(leftP + 1).Take(rightP - (leftP + 1)).ToArray());
  
        string[] split = paramsFull.Split(",");
        for(int i=0; i<split.Length; i++)
        {
            split[i] = TryEvaluate(split[i]);
        }
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

    public static async Task RunScript(string code)
    {
        string[] sp = code.Split('\n');
        ifStack = new Stack<bool>();
        executeLine = true;

        Stack<int> blockStack = new Stack<int>();
        for (int i = 0; i < sp.Length; i++)
        {
            string line = sp[i].Trim();
            if (line == "{")
            {
                blockStack.Push(i);
            }
            else if (line == "}")
            {
                int startBlock = blockStack.Pop();
                string[] blockLines = sp.Skip(startBlock + 1).Take(i - startBlock - 1).ToArray();
                await ExecuteBlock(blockLines);
                i = startBlock;
            }
            else
            {
                await ExecuteLine(line);
            }
        }
    }

    public static async Task ExecuteBlock(string[] lines)
    {
        foreach (string line in lines)
        {
            await ExecuteLine(line);
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

   
    public static string TryEvaluate(string value)
    {
        int firstQuote = value.IndexOf('"');
        int lastQuote = value.LastIndexOf('"');

        if (firstQuote != -1 && lastQuote != -1 && firstQuote != lastQuote)
        {
            string stringPart = value.Substring(firstQuote, lastQuote - firstQuote + 1).Trim('"');
            string beforeString = value.Substring(0, firstQuote).Trim();
            string afterString = value.Substring(lastQuote + 1).Trim();

            string evaluatedBefore = TryEvaluate(beforeString);
            string evaluatedAfter = TryEvaluate(afterString);

            if (evaluatedAfter.StartsWith("+"))
            {
                string afterValue = TryEvaluate(evaluatedAfter.Substring(1).Trim());
                return stringPart + afterValue;
            }
            else if (evaluatedAfter.StartsWith("*"))
            {
                string afterValue = TryEvaluate(evaluatedAfter.Substring(1).Trim());
                if (int.TryParse(afterValue, out int repeatCount))
                {
                    return string.Concat(Enumerable.Repeat(stringPart, repeatCount));
                }
            }

            return stringPart + evaluatedAfter; 
        }

        string[] operators = { "+", "-", "*", "/" };
        foreach (string op in operators)
        {
            int opPos = value.IndexOf(op);
            if (opPos > 0)
            {
                string left = value.Substring(0, opPos).Trim();
                string right = value.Substring(opPos + 1).Trim();
                string leftVal = TryEvaluate(left);
                string rightVal = TryEvaluate(right);

                return EvaluateOperator(op, leftVal, rightVal);
            }
        }

     
        foreach (string variable in variables.Keys.OrderByDescending(v => v.Length))
        {
            value = value.Replace(variable, variables[variable]);
        }

        return EvaluateConcatenation(value);
    }

    private static string EvaluateOperator(string op, string leftVal, string rightVal)
    {
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

        return ReturnError(ErrorType.InvalidVariable); // Default error case
    }

    private static string EvaluateConcatenation(string value)
    {
        string[] parts = value.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder result = new StringBuilder();

        foreach (string part in parts)
        {
            string trimmedPart = part.Trim();

            if (trimmedPart.StartsWith('"') && trimmedPart.EndsWith('"'))
            {
                result.Append(trimmedPart.Trim('"'));
            }
            else
            {
                result.Append(trimmedPart);
            }
        }

        return result.ToString();
    }

    public static Script LoadScript(string code)
    {
        Script script = new Script();
        string[] lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Stack<string> blockStack = new Stack<string>();
        StringBuilder currentBlock = new StringBuilder();
        string currentFunctionName = null;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("function"))
            {
                if (currentFunctionName != null)
                {
                    throw new Exception("Nested functions are not supported.");
                }
                currentFunctionName = trimmedLine.Split(' ')[1];
                blockStack.Push("{");
                currentBlock.AppendLine(trimmedLine);
            }
            else if (trimmedLine == "{" && blockStack.Count > 0)
            {
                blockStack.Push("{");
                currentBlock.AppendLine(trimmedLine);
            }
            else if (trimmedLine == "}" && blockStack.Count > 0)
            {
                blockStack.Pop();
                currentBlock.AppendLine(trimmedLine);

                if (blockStack.Count == 0 && currentFunctionName != null)
                {
                    Function function = new Function
                    {
                        body = currentBlock.ToString()
                    };
                    script.functions.Add(currentFunctionName, function);
                    currentBlock.Clear();
                    currentFunctionName = null;
                }
            }
            else
            {
                if (blockStack.Count > 0)
                {
                    currentBlock.AppendLine(trimmedLine);
                }
            }
        }

        if (blockStack.Count > 0)
        {
            throw new Exception("Mismatched braces in the script.");
        }

        return script;
    }

}