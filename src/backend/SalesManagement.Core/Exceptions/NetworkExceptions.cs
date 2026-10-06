namespace SalesManagement.Core.Exceptions;

public class NetworkValidationException(string message) : Exception(message);
public sealed class NetworkCycleException(string message = "순환 관계는 등록할 수 없습니다.") : NetworkValidationException(message);
public sealed class NetworkRelationshipConflictException(string message = "같은 거래선 관계의 유효기간이 중복됩니다.") : Exception(message);
