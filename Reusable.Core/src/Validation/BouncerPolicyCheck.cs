﻿using System.Collections.Generic;
using JetBrains.Annotations;
using Reusable.Extensions;

namespace Reusable.Validation
{
    // ReSharper disable once UnusedTypeParameter - We need the T to be able to chain extensions and pass the T to them.
//    public interface IWeelidationRuleResult<T>
//    {
//        [NotNull]
//        string Expression { get; }
//
//        bool Success { get; }
//
//        [CanBeNull]
//        string Message { get; }
//    }

    public class BouncerPolicyCheck<T> //: IWeelidationRuleResult<T>
    {
        // ReSharper disable once StaticMemberInGenericType - this is ok because it's common to all instances.
        private static readonly IDictionary<bool, string> ResultStrings = new Dictionary<bool, string>
        {
            [true] = "Passed",
            [false] = "Failed"
        };

        public BouncerPolicyCheck([NotNull] string expression, bool isFollowed, [NotNull] string message)
        {
            Expression = expression;
            IsFollowed = isFollowed;
            Message = message;
        }

        public string Expression { get; }

        public bool IsFollowed { get; }

        public string Message { get; }        

        public override string ToString() => $"{Expression} | {ResultStrings[IsFollowed]} ({Message ?? "N/A"})";

        public static implicit operator bool(BouncerPolicyCheck<T> policyCheck) => policyCheck.IsFollowed;
    }    
}