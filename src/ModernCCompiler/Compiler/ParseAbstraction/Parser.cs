﻿using Antlr4.Runtime;
using Compiler.ErrorHandling;
using Compiler.Models.Tree;

namespace Compiler.ParseAbstraction
{
    public static class Parser
    {
        /// <summary>
        /// Parses the input and returns an abstract syntax tree.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The abstract syntax tree.</returns>
        /// <exception cref="Exception"></exception>
        public static ProgramRoot Parse(string input)
        {
            // Lex
            var inputStream = new AntlrInputStream(input);
            var speakLexer = new ModernCLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(speakLexer);

            // Parse
            var modernCParser = new ModernCParser(commonTokenStream);
            var programContext = modernCParser.program();
            if (modernCParser.NumberOfSyntaxErrors > 0)
            {
                ErrorHandler.ParseError();
            }

            // Abstract
            var visitor = new ParseAbstractionVisitor();
            var tree = visitor.Visit(programContext);
            if (tree is ProgramRoot program)
            {
                return program;
            }

            throw new Exception($"Tried to parse {tree.GetType()} as program");
        }
    }
}
