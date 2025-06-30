using System.Text;
using SoftOmni.Parsing.Commons.CodeElements.Exceptions;
using SoftOmni.Parsing.Commons.Formatting.Parameters;
using SoftOmni.Parsing.Commons.ParsingResults;
using SoftOmni.Parsing.Commons.SegmentedStrings;

namespace SoftOmni.Parsing.Commons.CodeElements;

public abstract class CodeElement
{
    protected readonly Guid Id = Guid.NewGuid();

    protected IStringBuilder UnderlyingCode { get; }

    public abstract Func<IStringBuilder, ParsingResult> Validator { get; }

    public virtual Action<IStringBuilder, ReadOnlyFormattingParameters> Formatter { get; } = (code, parameters) => { }; 

    protected List<CodeElement> Children { get; }

    protected CodeElement? Parent { get; set; }

    protected internal int ParentIndex { get; protected set; }

    protected const int NoParentIndex = -1;

    protected CodeElement(IStringBuilder code)
    {
        UnderlyingCode = new SegmentedString();

        Children = [];
        Parent = null;
        ParentIndex = NoParentIndex;

        Code = code;
    }

    protected CodeElement(IStringBuilder code, bool doNotCheckValidity)
    {
        UnderlyingCode = new SegmentedString();

        Children = [];
        Parent = null;
        ParentIndex = NoParentIndex;

        if (!doNotCheckValidity)
        {
            Code = code;
        }
        else
        {
            UnderlyingCode.Append(code);
        }
    }

    protected CodeElement(CodeElement parent, int parentIndex, int startIndex, int length)
    {
        UnderlyingCode = new StringSegment(parent.Code, startIndex, length);

        ParsingResult checkResult = CheckCodeValidity();
        if (checkResult.IsFailure)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            string errorMessage = GenerateErrorMessage(UnderlyingCode, checkResult.Value, checkResult.Message);
            throw new InvalidCodeException(errorMessage);
        }

        Children = [];
        Parent = parent;
        ParentIndex = parentIndex;

        // ReSharper disable once VirtualMemberCallInConstructor
        BuildInternalStructures();

        Parent.Children.Insert(parentIndex, this);
    }

    protected CodeElement(CodeElement parent, int parentIndex, int startIndex, int length, bool doNotCheckValidity)
    {
        UnderlyingCode = new StringSegment(parent.Code, startIndex, length);

        if (doNotCheckValidity)
        {
            Children = [];
            Parent = parent;
            ParentIndex = parentIndex;

            // ReSharper disable once VirtualMemberCallInConstructor
            BuildInternalStructures();

            Parent.Children.Insert(parentIndex, this);
        }

        ParsingResult checkResult = CheckCodeValidity();
        if (checkResult.IsFailure)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            string errorMessage = GenerateErrorMessage(UnderlyingCode, checkResult.Value, checkResult.Message);
            throw new InvalidCodeException(errorMessage);
        }

        Children = [];
        Parent = parent;
        ParentIndex = parentIndex;

        // ReSharper disable once VirtualMemberCallInConstructor
        BuildInternalStructures();
        Parent.Children.Insert(parentIndex, this);
    }

    protected CodeElement(CodeElement parent, int parentIndex, IStringBuilder code, int childIndex)
    {
        UnderlyingCode = new StringSegment(parent.Code, childIndex);

        Children = [];
        Parent = parent;
        ParentIndex = parentIndex;

        Code = code;

        Parent.Children.Insert(parentIndex, this);
    }

    protected CodeElement(CodeElement parent, int parentIndex, IStringBuilder code, int childIndex,
        bool doNotCheckValidity)
    {
        UnderlyingCode = new StringSegment(parent.Code, childIndex);

        Children = [];
        Parent = parent;
        ParentIndex = parentIndex;

        if (doNotCheckValidity)
        {
            UnderlyingCode.Append(code);
        }
        else
        {
            Code = code;
        }

        Parent.Children.Insert(parentIndex, this);
    }

    protected CodeElement(CodeElement parent, IStringBuilder code)
        : this(parent, parent.Children.Count, code, parent.UnderlyingCode.Length)
    { }

    protected CodeElement(CodeElement parent, IStringBuilder code, bool doNotCheckValidity)
        : this(parent, parent.Children.Count, code, parent.UnderlyingCode.Length, doNotCheckValidity)
    { }

    public IStringBuilder Code
    {
        get => UnderlyingCode;
        
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        set
        {
            ParsingResult validityCheckResult = Validator(value);
            if (validityCheckResult.IsFailure)
            {
                string errorMessage =
                    GenerateErrorMessage(value, validityCheckResult.Value, validityCheckResult.Message);
                throw new InvalidCodeException(errorMessage);
            }

            UnderlyingCode.Clear();
            UnderlyingCode.Append(value);

            BuildInternalStructures();
        }
    }

    protected ParsingResult CheckCodeValidity(IStringBuilder code)
    {
        return Validator(code);
    }

    protected ParsingResult CheckCodeValidity()
    {
        return CheckCodeValidity(Code);
    }

    public bool IsValidCode(IStringBuilder code)
    {
        ParsingResult codeCheckResult = CheckCodeValidity(code);
        return codeCheckResult.IsSuccess;
    }

    protected virtual string GenerateErrorMessage(IStringBuilder code, int invalidIndex, string message)
    {
        StringBuilder errorMessageBuilder = new();
        errorMessageBuilder.AppendLine();
        errorMessageBuilder.AppendLine(code.ToString());

        StringBuilder leftPaddingBuilder = new();
        leftPaddingBuilder.Append(' ', invalidIndex);
        string leftPadding = leftPaddingBuilder.ToString();

        errorMessageBuilder.Append(leftPadding);
        errorMessageBuilder.Append('⇑');

        int numberOfCharsInCode = code.Length;
        int numberOfProblematicChars = numberOfCharsInCode - invalidIndex - 1;

        for (int i = 0; i < numberOfProblematicChars; i++)
        {
            errorMessageBuilder.Append('↑');
        }

        errorMessageBuilder.Append(leftPadding);
        errorMessageBuilder.Append('║');

        const string invalidCodeErrorMessage = "NOT READ BECAUSE OF FAULT: NOT A PART OF CODE ELEMENT";
        int invalidCodeErrorMessageLength = invalidCodeErrorMessage.Length;

        int leftPaddingOfErrorMessageAmount = 0;
        if (numberOfProblematicChars > invalidCodeErrorMessageLength)
        {
            int differenceToMakeUpOnBothSides = numberOfProblematicChars - invalidCodeErrorMessageLength;
            leftPaddingOfErrorMessageAmount = differenceToMakeUpOnBothSides / 2;
        }

        StringBuilder leftPaddingOfErrorMessageBuilder = new();

        for (int i = 0; i < leftPaddingOfErrorMessageAmount; i++)
        {
            leftPaddingOfErrorMessageBuilder.Append(' ');
        }

        string leftPaddingOfErrorMessage = leftPaddingOfErrorMessageBuilder.ToString();
        errorMessageBuilder.Append(leftPaddingOfErrorMessage);
        errorMessageBuilder.AppendLine(invalidCodeErrorMessage);

        errorMessageBuilder.Append(leftPadding);
        errorMessageBuilder.AppendLine("║");
        errorMessageBuilder.Append(leftPadding);
        errorMessageBuilder.Append('╚');
        errorMessageBuilder.Append(message);

        string errorMessage = errorMessageBuilder.ToString();
        return errorMessage;
    }

    protected abstract void BuildInternalStructures();
}