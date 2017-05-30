using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
//by huayun 2017-5-8 project of Fundamentals of Compiling
namespace myCompiler
{

    public enum TokenType
    {
        Comment,
        Keyword,
        Identifier,
        Operator,
        Delimiters,
        Numbers,
        Error
    }

    public enum Symbol
    {
        Terminal,
        NonTerminal,
        No
    }

    public struct symbol
    {
        public Token theTok;
        public string theNum;

        public symbol(Token tok, string num)
        {
            theTok = tok;
            theNum = num;
        }
    }

    public struct Token
    {
        public int RowNum { get; set; }
        public int Position { get; set; }
        public string TokenString { get; set; }
        public TokenType Type { get; set; }

        public Token(int row, int p, string token,TokenType mytype)
        {
            RowNum = row;
            Position = p;
            TokenString = token;
            Type = mytype;
        }

        public void PrintToken()
        {
            Console.WriteLine("Token: " + TokenString + " Line: " + (int)(RowNum+1) + " Position: " + Position + " Type: " + Type.ToString());
        }
    }

    public struct Sheet
    {
        public string Terminal;
        public string Nonterminal;
        public Production Product;

        public Sheet(string t, string nt, Production p)
        {
            Terminal = t;
            Nonterminal = nt;
            Product = p;
        }
    }

    public struct _Right
    {
        public int RightPos;
        public Symbol RightType;

        public _Right(int right, Symbol type)
        {
            RightPos = right;
            RightType = type;
        }
    }

    public struct Production
    {
        public int Left;
        public _Right[] Right;
        public Production(int left, _Right[] right)
        {
            Left = left;
            Right = right;
        }
    }
    public class LexicalAnalysis
    {
        public static Regex Keywords = new Regex(@"int|real|if|then|else|while");
        public static Regex Identifier = new Regex(@"(([A-Z]|[a-z])+)([0-9]*)");
        public static Regex Numbers = new Regex(@"-?[0-9]*[.]*[0-9]*");
        public static List<Token> lexicla_Analysis(string path)
        {
            var result = new List<Token>();
            try
            {
                using (var sw = new StreamReader(path))
                {
                    string lineString;
                    var lineNum = 0;
                    while ((lineString = sw.ReadLine()) != null)
                    {
                        Console.WriteLine(lineNum+1 + ": " + lineString);
                        var first = 0;
                        while (lineString[first] == ' ')
                        {
                            first++;
                        }
                        var i = first;
                        while(i < lineString.Length)
                        {
                            if ((i + 1) < lineString.Length && (lineString[i] == '/' && lineString[i + 1] == '/'))
                            {
                                var addToken = new Token(lineNum,i,lineString.Substring(i),TokenType.Comment);
                                result.Add(addToken);
                                break;
                            }
                            else if (lineString[i] == '+' || lineString[i] == '-' || lineString[i] == '*'
                                     || lineString[i] == '/' || lineString[i] == '<' || lineString[i] == '>' ||
                                     lineString[i] == '=' || lineString[i] == '!')
                            {
                                if ((i + 1) < lineString.Length && lineString[i + 1] == '=')
                                {
                                    var addToken = new Token(lineNum,i,lineString.Substring(i,2),TokenType.Operator);
                                    result.Add(addToken);
                                    i += 2;
                                }
                                else
                                {
                                    var addToken = new Token(lineNum,i,lineString.Substring(i,1),TokenType.Operator);
                                    result.Add(addToken);
                                    i++;
                                }
                            }
                            else if (lineString[i] == '(' || lineString[i] == ')' || lineString[i] == '{'
                                     || lineString[i] == '}' || lineString[i] == ';')
                            {
                                var addToken = new Token(lineNum,i,lineString.Substring(i,1),TokenType.Delimiters);
                                result.Add((addToken));
                                i++;
                            }
                            else if (lineString[i] == ' ')
                            {
                                i++;
                            }
                            else
                            {
                                var start = i;
                                for (var j = start+1; j < lineString.Length; j++)
                                {
                                    if (lineString[j] == ' ' || lineString[j] == ';' || lineString[j] == '{'
                                        || lineString[j] == '}' || lineString[j] == '(' || lineString[j] == ')'
                                        || lineString[j] == '+' || lineString[j] == '-' || lineString[j] == '*'
                                        || lineString[j] == '/' || lineString[j] == '>' || lineString[j] == '<'
                                        || lineString[j] == '=' || lineString[j] == '!')
                                    {
                                        var end = j;
                                        TokenType myType;
                                        var addString = lineString.Substring(start, end - start);
                                        if(Keywords.IsMatch(addString)) myType = TokenType.Keyword;
                                        else if(Identifier.IsMatch(addString)) myType = TokenType.Identifier;
                                        else if(Numbers.IsMatch(addString)) myType = TokenType.Numbers;
                                        else myType = TokenType.Error;
                                        var addToken = new Token(lineNum,start,addString,myType);
                                        result.Add(addToken);
                                        i = end;
                                        break;
                                    }
                                }
                            }
                        }
                        lineNum++;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return result;
        }
    }

    public class GrammerAnalysis
    {
        private List<string> _terminal = new List<string>();
        private List<string> _nonTerminal = new List<string>();
        public static List<Production> Productions = new List<Production>();
        private List<Token> _myTokenList;
        private Stack<_Right> _solutionStack = new Stack<_Right>();
        private List<Sheet> _solutionSheet = new List<Sheet>();
        private List<Token> error_sheet = new List<Token>();
        private List<symbol> symbols_sheet = new List<symbol>();

        private void IntilizesymbolSheet()
        {
            for (int i = 0; i < _myTokenList.Count; i++)
            {
                if (_myTokenList[i].Type == TokenType.Identifier && _myTokenList[i + 1].TokenString == "=")
                {
                    if (_myTokenList[i + 2].Type == TokenType.Numbers)
                    {
                        bool isIn = false;
                        foreach (var item in symbols_sheet)
                        {
                            if (_myTokenList[i].TokenString == item.theTok.ToString())
                                isIn = true;
                        }
                        if (isIn == false)
                        {
                            symbols_sheet.Add(new symbol(_myTokenList[i], "int"));
                        }
                    }
                }
            }
        }
        private void IntilizeTerminal()
        {
            _terminal.Add("if");//0
            _terminal.Add("then");//1
            _terminal.Add("(");//2
            _terminal.Add(")");//3
            _terminal.Add("else");//4
            _terminal.Add("while");//5
            _terminal.Add("Identifier");//6
            _terminal.Add("<");//7
            _terminal.Add("<=");//8
            _terminal.Add(">");//9
            _terminal.Add(">=");//10
            _terminal.Add("==");//11
            _terminal.Add("+");//12
            _terminal.Add("-");//13
            _terminal.Add("*");//14
            _terminal.Add("/");//15
            _terminal.Add("Numbers");//16
            _terminal.Add("{");//17
            _terminal.Add("}");//18
            _terminal.Add("=");//19
            _terminal.Add("e");//20
            _terminal.Add("$");//21
            _terminal.Add(";");//22
        }

        private void IntilizeNonterminal()
        {
            _nonTerminal.Add("program");//0
            _nonTerminal.Add("stmt");//1
            _nonTerminal.Add("compoundstmt");//2
            _nonTerminal.Add("stmts");//3
            _nonTerminal.Add("ifstmt");//4
            _nonTerminal.Add("whilestmt");//5
            _nonTerminal.Add("assgstmt");//6
            _nonTerminal.Add("boolexpr");//7
            _nonTerminal.Add("arithexpr");//8
            _nonTerminal.Add("arithexprprime");//9
            _nonTerminal.Add("multexpr");//10
            _nonTerminal.Add("multexprprime");//11
            _nonTerminal.Add("simpleexpr");//12
            _nonTerminal.Add("boolop");//13
        }

        private void IntilizeProductions()
        {
            Production myProduction;
            _Right[] myRight;
            myRight = new[] {new _Right(2, Symbol.NonTerminal)};
            myProduction = new Production(0,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(4, Symbol.NonTerminal)};
            myProduction = new Production(1,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(5, Symbol.NonTerminal)};
            myProduction = new Production(1,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(6, Symbol.NonTerminal)};
            myProduction = new Production(1,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(2, Symbol.NonTerminal)};
            myProduction = new Production(1,myRight);
            Productions.Add(myProduction);
            myRight = new[]
                {new _Right(17, Symbol.Terminal), new _Right(3, Symbol.NonTerminal), new _Right(18, Symbol.Terminal)};
            myProduction = new Production(2,myRight);
            Productions.Add(myProduction);
            myRight = new[]
                {new _Right(1, Symbol.NonTerminal), new _Right(3, Symbol.NonTerminal)};
            myProduction = new Production(3,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(20, Symbol.No)};
            myProduction = new Production(3,myRight);
            Productions.Add(myProduction);
            myRight = new[]
            {
                new _Right(0, Symbol.Terminal), new _Right(2, Symbol.Terminal), new _Right(7, Symbol.NonTerminal),
                new _Right(3, Symbol.Terminal),
                new _Right(1, Symbol.Terminal), new _Right(1, Symbol.NonTerminal), new _Right(4, Symbol.Terminal),
                new _Right(1, Symbol.NonTerminal)
            };
            myProduction = new Production(4,myRight);
            Productions.Add(myProduction);
            myRight = new[]
            {
                new _Right(5, Symbol.Terminal), new _Right(2, Symbol.Terminal), new _Right(7, Symbol.NonTerminal),
                new _Right(3, Symbol.Terminal),
                new _Right(1, Symbol.NonTerminal)
            };
            myProduction = new Production(5,myRight);
            Productions.Add(myProduction);
            myRight = new[]
                {new _Right(6, Symbol.Terminal), new _Right(19, Symbol.Terminal), new _Right(8, Symbol.NonTerminal),new _Right(22,Symbol.Terminal)};
            myProduction = new Production(6,myRight);
            Productions.Add(myProduction);
            myRight = new[]
            {
                new _Right(8, Symbol.NonTerminal), new _Right(13, Symbol.NonTerminal), new _Right(8, Symbol.NonTerminal)
            };
            myProduction = new Production(7,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(7, Symbol.Terminal)};
            myProduction = new Production(13,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(9, Symbol.Terminal)};
            myProduction = new Production(13,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(8, Symbol.Terminal)};
            myProduction = new Production(13,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(10, Symbol.Terminal)};
            myProduction = new Production(13,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(11, Symbol.Terminal)};
            myProduction = new Production(13,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(10, Symbol.NonTerminal), new _Right(9, Symbol.NonTerminal)};
            myProduction = new Production(8, myRight);
            Productions.Add(myProduction);
            myRight = new[]
                {new _Right(12, Symbol.Terminal), new _Right(10, Symbol.NonTerminal), new _Right(9, Symbol.NonTerminal)};
            myProduction = new Production(9,myRight);
            Productions.Add(myProduction);
            myRight = new[]
                {new _Right(13, Symbol.Terminal), new _Right(10, Symbol.NonTerminal), new _Right(9, Symbol.NonTerminal)};
            myProduction = new Production(9,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(20,Symbol.No)};
            myProduction = new Production(9,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(12, Symbol.NonTerminal), new _Right(11, Symbol.NonTerminal)};
            myProduction = new Production(10,myRight);
            Productions.Add(myProduction);
            myRight = new[]
                {new _Right(14, Symbol.Terminal), new _Right(12, Symbol.NonTerminal), new _Right(11, Symbol.NonTerminal)};
            myProduction = new Production(11,myRight);
            Productions.Add(myProduction);
            myRight = new[]
                {new _Right(15, Symbol.Terminal), new _Right(12, Symbol.NonTerminal), new _Right(11, Symbol.NonTerminal)};
            myProduction = new Production(11,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(20,Symbol.No)};
            myProduction = new Production(11,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(6,Symbol.Terminal)};
            myProduction = new Production(12,myRight);
            Productions.Add(myProduction);
            myRight = new[] {new _Right(16,Symbol.Terminal)};
            myProduction = new Production(12,myRight);
            Productions.Add(myProduction);
            myRight = new[]
                {new _Right(2, Symbol.Terminal), new _Right(8, Symbol.NonTerminal), new _Right(3, Symbol.Terminal)};
            myProduction = new Production(12,myRight);
            Productions.Add(myProduction);
        }

        private void IntilizeSheet()
        {
            _solutionSheet.Add(new Sheet(_terminal[0],_nonTerminal[1],Productions[1]));
            _solutionSheet.Add(new Sheet(_terminal[0],_nonTerminal[3],Productions[6]));
            _solutionSheet.Add(new Sheet(_terminal[0],_nonTerminal[4],Productions[8]));
            _solutionSheet.Add(new Sheet(_terminal[2],_nonTerminal[7],Productions[11]));
            _solutionSheet.Add(new Sheet(_terminal[2],_nonTerminal[10],Productions[21]));
            _solutionSheet.Add(new Sheet(_terminal[2],_nonTerminal[12],Productions[27]));
            _solutionSheet.Add(new Sheet(_terminal[2],_nonTerminal[8],Productions[17]));
            _solutionSheet.Add(new Sheet(_terminal[3],_nonTerminal[9],Productions[20]));
            _solutionSheet.Add(new Sheet(_terminal[3],_nonTerminal[11],Productions[24]));
            _solutionSheet.Add(new Sheet(_terminal[5],_nonTerminal[1],Productions[2]));
            _solutionSheet.Add(new Sheet(_terminal[5],_nonTerminal[3],Productions[6]));
            _solutionSheet.Add(new Sheet(_terminal[5],_nonTerminal[5],Productions[9]));
            _solutionSheet.Add(new Sheet(_terminal[6],_nonTerminal[1],Productions[3]));
            _solutionSheet.Add(new Sheet(_terminal[6],_nonTerminal[3],Productions[6]));
            _solutionSheet.Add(new Sheet(_terminal[6],_nonTerminal[6],Productions[10]));
            _solutionSheet.Add(new Sheet(_terminal[6],_nonTerminal[7],Productions[11]));
            _solutionSheet.Add(new Sheet(_terminal[6],_nonTerminal[10],Productions[21]));
            _solutionSheet.Add(new Sheet(_terminal[6],_nonTerminal[12],Productions[25]));
            _solutionSheet.Add(new Sheet(_terminal[6],_nonTerminal[8],Productions[17]));
            _solutionSheet.Add(new Sheet(_terminal[7],_nonTerminal[9],Productions[20]));
            _solutionSheet.Add(new Sheet(_terminal[7],_nonTerminal[11],Productions[24]));
            _solutionSheet.Add(new Sheet(_terminal[7],_nonTerminal[13],Productions[12]));
            _solutionSheet.Add(new Sheet(_terminal[8],_nonTerminal[9],Productions[20]));
            _solutionSheet.Add(new Sheet(_terminal[8],_nonTerminal[11],Productions[24]));
            _solutionSheet.Add(new Sheet(_terminal[8],_nonTerminal[13],Productions[14]));
            _solutionSheet.Add(new Sheet(_terminal[9],_nonTerminal[9],Productions[20]));
            _solutionSheet.Add(new Sheet(_terminal[9],_nonTerminal[11],Productions[24]));
            _solutionSheet.Add(new Sheet(_terminal[9],_nonTerminal[13],Productions[13]));
            _solutionSheet.Add(new Sheet(_terminal[10],_nonTerminal[9],Productions[20]));
            _solutionSheet.Add(new Sheet(_terminal[10],_nonTerminal[11],Productions[24]));
            _solutionSheet.Add(new Sheet(_terminal[10],_nonTerminal[13],Productions[15]));
            _solutionSheet.Add(new Sheet(_terminal[11],_nonTerminal[9],Productions[20]));
            _solutionSheet.Add(new Sheet(_terminal[11],_nonTerminal[11],Productions[24]));
            _solutionSheet.Add(new Sheet(_terminal[11],_nonTerminal[13],Productions[16]));
            _solutionSheet.Add(new Sheet(_terminal[12],_nonTerminal[9],Productions[18]));
            _solutionSheet.Add(new Sheet(_terminal[12],_nonTerminal[11],Productions[24]));
            _solutionSheet.Add(new Sheet(_terminal[13],_nonTerminal[9],Productions[19]));
            _solutionSheet.Add(new Sheet(_terminal[13],_nonTerminal[11],Productions[24]));
            _solutionSheet.Add(new Sheet(_terminal[14],_nonTerminal[11],Productions[22]));
            _solutionSheet.Add(new Sheet(_terminal[15],_nonTerminal[11],Productions[23]));
            _solutionSheet.Add(new Sheet(_terminal[16],_nonTerminal[7],Productions[11]));
            _solutionSheet.Add(new Sheet(_terminal[16],_nonTerminal[10],Productions[21]));
            _solutionSheet.Add(new Sheet(_terminal[16],_nonTerminal[12],Productions[26]));
            _solutionSheet.Add(new Sheet(_terminal[16],_nonTerminal[8],Productions[17]));
            _solutionSheet.Add(new Sheet(_terminal[17],_nonTerminal[0],Productions[0]));
            _solutionSheet.Add(new Sheet(_terminal[17],_nonTerminal[2],Productions[5]));
            _solutionSheet.Add(new Sheet(_terminal[17],_nonTerminal[3],Productions[6]));
            _solutionSheet.Add(new Sheet(_terminal[17],_nonTerminal[1],Productions[4]));
            _solutionSheet.Add(new Sheet(_terminal[18],_nonTerminal[3],Productions[7]));
            _solutionSheet.Add(new Sheet(_terminal[22],_nonTerminal[11],Productions[24]));
            _solutionSheet.Add(new Sheet(_terminal[22],_nonTerminal[9],Productions[20]));
        }

        public GrammerAnalysis(List<Token> result)
        {
            IntilizeTerminal();
            IntilizeNonterminal();
            IntilizeProductions();
            IntilizeSheet();
            _myTokenList = result;
            IntilizesymbolSheet();
        }

        public void print_Symbol_sheet()
        {
            Console.WriteLine("Symbol Table");
            foreach (var symbol in symbols_sheet)
            {
                Console.WriteLine(symbol.theNum + ": " + symbol.theTok.TokenString + " Line: " + symbol.theTok.RowNum +
                                  " Position: " + symbol.theTok.Position);
            }
        }

        public void Print_Error()
        {
            Console.WriteLine(error_sheet.Count + " ERRORS");
            foreach (var error in error_sheet)
            {
                error.PrintToken();
            }
        }

        public void Print_Productions()
        {
            int n = 0;
            foreach (var production in Productions)
            {
                Console.Write(n +". "+ _nonTerminal[production.Left] + " -->  ");
                foreach (var tok in production.Right)
                {
                    string tmp;
                    if (tok.RightType == Symbol.Terminal)
                        tmp = _terminal[tok.RightPos];
                    else if (tok.RightType == Symbol.No)
                        tmp = _terminal[20];
                    else
                        tmp = _nonTerminal[tok.RightPos];
                    Console.Write(tmp+" ");
                }
                Console.Write("\n");
                n++;
            }
        }
        public void Analysis()
        {
            _solutionStack.Push(new _Right(0,Symbol.NonTerminal));
            Console.WriteLine("Analuysis Begin:");
            foreach (var token in _myTokenList)
            {
                int count = 0;
                bool isFind = false;
                bool isput_in_error_stack = false;
                string tok = return_token_type(token);
                while (!(_solutionStack.Peek().RightType == Symbol.Terminal &&
                         _terminal[_solutionStack.Peek().RightPos] == tok))
                {
                    //var take = from item in _solutionSheet where item.Terminal == tok select item;
                    var tmp = new Sheet();
                    foreach (var item in _solutionSheet)
                    {
                        int i = _solutionStack.Peek().RightPos;
                        if (item.Terminal == tok && i <= 13 && item.Nonterminal == _nonTerminal[i])
                        {
                            tmp = item;
                            isFind = true;
                            break;
                        }
                    }
                    if (isFind == false)
                    {
                        error_sheet.Add(token);
                        isput_in_error_stack = true;
                        break;
                    }
                    else
                    {
                        var test = _solutionStack.Pop();
                        //foreach (var item in tmp.Product.Right)
                        for (int i = tmp.Product.Right.Length - 1; i > -1; i--)
                        {
                            if (tmp.Product.Right[i].RightType == Symbol.No)
                                break;
                            _solutionStack.Push(tmp.Product.Right[i]);
                        }
                        if (count == 0)
                        {
                            Console.Write("Now is the ");
                            token.PrintToken();
                            count++;
                        }
                        Console.Write("    "+_nonTerminal[tmp.Product.Left] + " --> ");
                        foreach (var t in tmp.Product.Right)
                        {
                            string Tmp;
                            if (t.RightType == Symbol.Terminal)
                                Tmp = _terminal[t.RightPos];
                            else if (t.RightType == Symbol.No)
                                Tmp = _terminal[20];
                            else
                                Tmp = _nonTerminal[t.RightPos];
                            Console.Write(Tmp + " ");
                        }
                        Console.Write("\n");
                    }
                }
                if(isFind == false && isput_in_error_stack == true) continue;
                Console.WriteLine("match: " + _terminal[_solutionStack.Peek().RightPos]+"\n");
                _solutionStack.Pop();
            }
        }

        private string return_token_type(Token tok)
        {
            if (tok.Type == TokenType.Identifier || tok.Type == TokenType.Numbers)
                return tok.Type.ToString();
            else
            {
                return tok.TokenString;
            }
        }

    }
    internal class Program
    {
        public static void Main(string[] args)
        {

            var result = LexicalAnalysis.lexicla_Analysis("./test.txt");
            foreach (var tok in result)
            {
                tok.PrintToken();
            }
            var ga = new GrammerAnalysis(result);
            ga.Print_Productions();
            ga.Analysis();
            Console.WriteLine();
            ga.Print_Error();
            Console.WriteLine();
            ga.print_Symbol_sheet();
            Console.WriteLine("by huayun 2017-5-8");
        }
    }
}