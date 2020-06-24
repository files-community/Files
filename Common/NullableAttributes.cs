#if !NULLABLE_ATTRIBUTES_DISABLE
#nullable enable
#pragma warning disable

namespace System.Diagnostics.CodeAnalysis
{
    using global::System;

    /// <summary>
    ///     Specifies that <see langword="null"/> is allowed as an input even if the
    ///     corresponding type disallows it.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property,
        Inherited = false
    )]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
    [DebuggerNonUserCode]
#endif
    public sealed class AllowNullAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AllowNullAttribute"/> class.
        /// </summary>
        public AllowNullAttribute() { }
    }

    /// <summary>
    ///     Specifies that <see langword="null"/> is disallowed as an input even if the
    ///     corresponding type allows it.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property,
        Inherited = false
    )]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
    [DebuggerNonUserCode]
#endif
    public sealed class DisallowNullAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DisallowNullAttribute"/> class.
        /// </summary>
        public DisallowNullAttribute() { }
    }

    /// <summary>
    ///     Specifies that a method that will never return under any circumstance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
    [DebuggerNonUserCode]
#endif
    public sealed class DoesNotReturnAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DoesNotReturnAttribute"/> class.
        /// </summary>
        ///
        public DoesNotReturnAttribute() { }
    }

    /// <summary>
    ///     Specifies that the method will not return if the associated <see cref="Boolean"/>
    ///     parameter is passed the specified value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
    [DebuggerNonUserCode]
#endif
    public sealed class DoesNotReturnIfAttribute : Attribute
    {
        /// <summary>
        ///     Gets the condition parameter value.
        ///     Code after the method is considered unreachable by diagnostics if the argument
        ///     to the associated parameter matches this value.
        /// </summary>
        public bool ParameterValue { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DoesNotReturnIfAttribute"/>
        ///     class with the specified parameter value.
        /// </summary>
        /// <param name="parameterValue">
        ///     The condition parameter value.
        ///     Code after the method is considered unreachable by diagnostics if the argument
        ///     to the associated parameter matches this value.
        /// </param>
        public DoesNotReturnIfAttribute(bool parameterValue)
        {
            ParameterValue = parameterValue;
        }
    }

    /// <summary>
    ///     Specifies that an output may be <see langword="null"/> even if the
    ///     corresponding type disallows it.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Parameter |
        AttributeTargets.Property | AttributeTargets.ReturnValue,
        Inherited = false
    )]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
    [DebuggerNonUserCode]
#endif
    public sealed class MaybeNullAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MaybeNullAttribute"/> class.
        /// </summary>
        public MaybeNullAttribute() { }
    }

    /// <summary>
    ///     Specifies that when a method returns <see cref="ReturnValue"/>, 
    ///     the parameter may be <see langword="null"/> even if the corresponding type disallows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
    [DebuggerNonUserCode]
#endif
    public sealed class MaybeNullWhenAttribute : Attribute
    {
        /// <summary>
        ///     Gets the return value condition.
        ///     If the method returns this value, the associated parameter may be <see langword="null"/>.
        /// </summary>
        public bool ReturnValue { get; }

        /// <summary>
        ///      Initializes the attribute with the specified return value condition.
        /// </summary>
        /// <param name="returnValue">
        ///     The return value condition.
        ///     If the method returns this value, the associated parameter may be <see langword="null"/>.
        /// </param>
        public MaybeNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }
    }

    /// <summary>
    ///     Specifies that an output is not <see langword="null"/> even if the
    ///     corresponding type allows it.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Parameter |
        AttributeTargets.Property | AttributeTargets.ReturnValue,
        Inherited = false
    )]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
    [DebuggerNonUserCode]
#endif
    public sealed class NotNullAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NotNullAttribute"/> class.
        /// </summary>
        public NotNullAttribute() { }
    }

    /// <summary>
    ///     Specifies that the output will be non-<see langword="null"/> if the
    ///     named parameter is non-<see langword="null"/>.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue,
        AllowMultiple = true,
        Inherited = false
    )]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
    [DebuggerNonUserCode]
#endif
    public sealed class NotNullIfNotNullAttribute : Attribute
    {
        /// <summary>
        ///     Gets the associated parameter name.
        ///     The output will be non-<see langword="null"/> if the argument to the
        ///     parameter specified is non-<see langword="null"/>.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        ///     Initializes the attribute with the associated parameter name.
        /// </summary>
        /// <param name="parameterName">
        ///     The associated parameter name.
        ///     The output will be non-<see langword="null"/> if the argument to the
        ///     parameter specified is non-<see langword="null"/>.
        /// </param>
        public NotNullIfNotNullAttribute(string parameterName)
        {
            // .NET Core 3.0 doesn't throw an ArgumentNullException, even though this is
            // tagged as non-null.
            // Follow this behavior here.
            ParameterName = parameterName;
        }
    }

    /// <summary>
    ///     Specifies that when a method returns <see cref="ReturnValue"/>,
    ///     the parameter will not be <see langword="null"/> even if the corresponding type allows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
#if !NULLABLE_ATTRIBUTES_INCLUDE_IN_CODE_COVERAGE
    [DebuggerNonUserCode]
#endif
    public sealed class NotNullWhenAttribute : Attribute
    {
        /// <summary>
        ///     Gets the return value condition.
        ///     If the method returns this value, the associated parameter will not be <see langword="null"/>.
        /// </summary>
        public bool ReturnValue { get; }

        /// <summary>
        ///     Initializes the attribute with the specified return value condition.
        /// </summary>
        /// <param name="returnValue">
        ///     The return value condition.
        ///     If the method returns this value, the associated parameter will not be <see langword="null"/>.
        /// </param>
        public NotNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }
    }
}

#pragma warning enable
#nullable restore
#endif // NULLABLE_ATTRIBUTES_DISABLE
