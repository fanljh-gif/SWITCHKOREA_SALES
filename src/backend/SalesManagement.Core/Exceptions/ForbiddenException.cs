namespace SalesManagement.Core.Exceptions;
public sealed class ForbiddenException(string message = "이 작업을 수행할 권한이 없습니다.") : Exception(message);
