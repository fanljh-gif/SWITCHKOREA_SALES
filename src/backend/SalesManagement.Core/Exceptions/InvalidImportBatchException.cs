namespace SalesManagement.Core.Exceptions;

public sealed class InvalidImportBatchException(string message = "검증 오류가 있는 가져오기 배치는 반영할 수 없습니다.") : Exception(message);
