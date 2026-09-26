// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Arc.Visceral;

/// <summary>
/// Builds indented source text with disposable namespace and block scopes.
/// </summary>
public class ScopingStringBuilder
{
    public const int MaxIndentSpaces = 16;
    public const int MaxIndent = 32;

    public ScopingStringBuilder(int indentSpaces = 4)
    {
        this.CurrentScope = new Scope(this, false, false);

        // Indent spaces ranges from 0 to MaxIndentSpaces.
        this.IndentSpaces = indentSpaces < MaxIndentSpaces ? indentSpaces : MaxIndentSpaces;
        this.IndentSpaces = this.IndentSpaces > 0 ? this.IndentSpaces : 0;

        // Create indent string cache.
        this.IndentString = new string(' ', this.IndentSpaces);
    }

    /// <summary>
    /// Gets the number of indent spaces.
    /// </summary>
    public int IndentSpaces { get; }

    /// <summary>
    /// Gets the cached indent string.
    /// </summary>
    public string IndentString { get; }

    public IScope CurrentScope { get; private set; }

    public string CurrentObject => this.CurrentScope.CurrentObject;

    public string FullObject => this.CurrentScope.FullObject;

    public bool AddUsing(string @namespace)
    {
        if (@namespace == "System" || @namespace.StartsWith("System."))
        { // For sorting purpose.
            return this.usingSystem.Add(@namespace);
        }
        else
        { // Other namespaces.
            return this.usingOther.Add(@namespace);
        }
    }

    public void AddHeader(string header)
    {
        this.header.Add(header);
    }

    public void AppendNamespace(string @namespace)
    {
        if (!string.IsNullOrEmpty(@namespace))
        {
            this.AppendLine($"namespace {@namespace};");
            this.AppendLine();
        }
    }

    public IScope ScopeNamespace(string @namespace)
    {
        if (!string.IsNullOrEmpty(@namespace))
        {
            return this.ScopeBrace($"namespace {@namespace}");
        }
        else
        {
            return new Scope(this, false, false);
        }
    }

    public IScope ScopeBrace(string preface)
    {
        if (preface != null)
        {
            this.AppendLine(preface);
        }

        this.Append("{\r\n");
        return new Scope(this, true, true);
    }

    /// <summary>
    /// Appends an interpolated preface line directly to the buffer and opens a brace scope.
    /// </summary>
    /// <param name="preface">The interpolated preface, written to the buffer as it is formatted.</param>
    /// <returns>The opened scope.</returns>
    public IScope ScopeBrace([InterpolatedStringHandlerArgument("")] ref LineHandler preface)
    {
        this.Append("\r\n", false);
        this.Append("{\r\n");
        return new Scope(this, true, true);
    }

    public IScope ScopeObject(string objectName, bool addPeriod = true) => new Scope(this, objectName, addPeriod);

    public IScope ScopeFullObject(string fullObjectName) => new Scope(this, fullObjectName);

    public void Append(string text, bool indentFlag = true)
    {
        this.ThrowIfDisposed();
        if (indentFlag)
        {
            this.AppendIndent();
        }

        this.sb.Append(text);
        return;
    }

    public void AppendLine(string? text = null, bool indentFlag = true)
    {
        this.ThrowIfDisposed();
        if (text != null)
        {
            this.Append(text, indentFlag);
        }

        this.Append("\r\n", false);
    }

    /// <summary>
    /// Appends an indented interpolated line directly to the buffer, without creating an intermediate string.
    /// </summary>
    /// <param name="handler">The interpolated line, written to the buffer as it is formatted.</param>
    public void AppendLine([InterpolatedStringHandlerArgument("")] ref LineHandler handler)
    {
        this.Append("\r\n", false);
    }

    public void Append(ScopingStringBuilder ssb)
    {
        var list = ssb.sb.ToString().Replace("\r\n", "\n").Split(['\n', '\r']);
        if (list == null)
        {
            return;
        }

        for (var i = 0; i < list.Length; i++)
        {
            if (i == (list.Length - 1) && list[i].Length == 0)
            {
                break;
            }

            this.AppendLine(list[i]);
        }
    }

    public int IncrementIndent() => this.CurrentScope.IncrementIndent();

    public int DecrementIndent() => this.CurrentScope.DecrementIndent();

    /// <summary>
    /// Finalize and get the result. All scopes will be disposed.
    /// </summary>
    /// <returns>A result string.</returns>
    public string Finalize()
    {
        while (this.CurrentScope.Parent != null)
        {
            this.CurrentScope.Dispose();
        }

        var s = new StringBuilder();

        foreach (var x in this.header)
        {
            s.Append(x);
            s.Append("\r\n");
        }

        foreach (var x in this.usingSystem)
        {
            s.Append("using ");
            s.Append(x);
            s.Append(";\r\n");
        }

        foreach (var x in this.usingOther)
        {
            s.Append("using ");
            s.Append(x);
            s.Append(";\r\n");
        }

        if (this.header.Count > 0 || this.usingSystem.Count > 0 || this.usingOther.Count > 0)
        {
            s.Append("\r\n");
        }

        // Prepend the small prefix instead of copying the whole body into another builder.
        this.sb.Insert(0, s.ToString());
        var result = this.sb.ToString();
        this.sb.Clear();
        this.header.Clear();
        this.usingSystem.Clear();
        this.usingOther.Clear();

        return result;
    }

    private void ThrowIfDisposed()
    {
        if (this.CurrentScope.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(Scope));
        }
    }

    private void AppendIndent()
    {
        var n = this.CurrentScope.CurrentIndent < MaxIndent ? this.CurrentScope.CurrentIndent : MaxIndent;
        while (n-- > 0)
        {
            this.sb.Append(this.IndentString);
        }
    }

    private StringBuilder sb = new StringBuilder();
    private List<string> header = new();
    private SortedSet<string> usingSystem = new();
    private SortedSet<string> usingOther = new();

    /// <summary>
    /// Writes an indented interpolated line directly to the builder; formatting matches default string interpolation.
    /// </summary>
    [InterpolatedStringHandler]
    public readonly struct LineHandler
    {
        private readonly StringBuilder sb;

        public LineHandler(int literalLength, int formattedCount, ScopingStringBuilder ssb)
        {
            ssb.ThrowIfDisposed();
            ssb.AppendIndent();
            this.sb = ssb.sb;
        }

        public void AppendLiteral(string value) => this.sb.Append(value);

        public void AppendFormatted(string? value) => this.sb.Append(value);

        public void AppendFormatted<T>(T value) => this.AppendFormatted(value, null);

        public void AppendFormatted<T>(T value, string? format)
        {
            if (value is IFormattable formattable)
            {
                this.sb.Append(formattable.ToString(format, null));
            }
            else if (value is not null)
            {
                this.sb.Append(value.ToString());
            }
        }
    }

    /// <summary>
    /// Closes a generated code scope when disposed.
    /// </summary>
    public class Scope : IScope
    {
        public Scope(ScopingStringBuilder ssb, bool hasBrace, bool indentFlag)
        { // Brace scope
            this.ssb = ssb;
            this.Parent = this.ssb.CurrentScope;
            this.ssb.CurrentScope = this;

            this.HasBrace = hasBrace;
            if (this.Parent == null)
            {
                this.CurrentIndent = 0;
                this.FullObject = string.Empty;
            }
            else
            {
                this.CurrentIndent = this.Parent.CurrentIndent;
                this.FullObject = this.Parent.FullObject;
            }

            if (indentFlag)
            {
                this.CurrentIndent++;
            }

            this.CurrentObject = string.Empty;
        }

        public Scope(ScopingStringBuilder ssb, string objectName, bool addPeriod)
        { // Object scope
            this.ssb = ssb;
            this.Parent = this.ssb.CurrentScope;
            this.ssb.CurrentScope = this;

            this.HasBrace = false;
            this.CurrentObject = objectName;
            if (this.Parent == null)
            {
                this.CurrentIndent = 0;
                this.FullObject = this.CurrentObject;
            }
            else
            {
                this.CurrentIndent = this.Parent.CurrentIndent;
                if (this.Parent.FullObject == string.Empty)
                {
                    this.FullObject = objectName;
                }
                else
                {
                    if (addPeriod)
                    {
                        this.FullObject = this.Parent.FullObject + "." + this.CurrentObject;
                    }
                    else
                    {
                        this.FullObject = this.Parent.FullObject + this.CurrentObject;
                    }
                }
            }
        }

        public Scope(ScopingStringBuilder ssb, string fullObjectName)
        { // FullObject scope
            this.ssb = ssb;
            this.Parent = this.ssb.CurrentScope;
            this.ssb.CurrentScope = this;

            this.HasBrace = false;
            this.CurrentObject = fullObjectName;
            if (this.Parent == null)
            {
                this.CurrentIndent = 0;
                this.FullObject = this.CurrentObject;
            }
            else
            {
                this.CurrentIndent = this.Parent.CurrentIndent;
                this.FullObject = this.CurrentObject;
            }
        }

        public int IncrementIndent() => ++this.CurrentIndent;

        public int DecrementIndent() => this.CurrentIndent == 0 ? 0 : --this.CurrentIndent;

        public IScope? Parent { get; }

        public int CurrentIndent { get; private set; }

        public bool HasBrace { get; }

        public string CurrentObject { get; }

        public string FullObject { get; }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (this.IsDisposed || this.Parent == null)
            { // Already disposed or the root scope (the root scope cannot be disposed).
                return;
            }

            if (this.ssb.CurrentScope != this)
            {
                throw new InvalidOperationException("The disposal order of the scopes should be the reverse order in which they are created.");
            }

            this.ssb.CurrentScope = this.Parent;
            if (this.HasBrace)
            {
                this.ssb.Append("}\r\n");
            }

            this.IsDisposed = true;
        }

        private ScopingStringBuilder ssb;
    }

    /// <summary>
    /// Represents a disposable scope in generated source text.
    /// </summary>
    public interface IScope : IDisposable
    {
        bool IsDisposed { get; }

        IScope? Parent { get; }

        int CurrentIndent { get; }

        bool HasBrace { get; }

        string CurrentObject { get; }

        string FullObject { get; }

        int IncrementIndent();

        int DecrementIndent();
    }
}
