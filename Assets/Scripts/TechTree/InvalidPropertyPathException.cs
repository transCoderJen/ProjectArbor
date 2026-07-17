using System;

namespace ShiftedSignal.Garden.TechTree
{
    public class InvalidPropertyPathException : Exception
    {
        public string PropertyPath { get; }
        public string InvalidPropertyName { get; }
        public Type TargetType { get; }

        public InvalidPropertyPathException(
            string propertyPath,
            string invalidPropertyName,
            Type targetType)
            : base(
                $"Unable to resolve property path '{propertyPath}'. " +
                $"Property '{invalidPropertyName}' was not found on " +
                $"type '{targetType?.FullName ?? "null"}'.")
        {
            PropertyPath = propertyPath;
            InvalidPropertyName = invalidPropertyName;
            TargetType = targetType;
        }

        public InvalidPropertyPathException(
            string propertyPath,
            string message)
            : base(
                $"Unable to resolve property path '{propertyPath}'. {message}")
        {
            PropertyPath = propertyPath;
        }
    }
}