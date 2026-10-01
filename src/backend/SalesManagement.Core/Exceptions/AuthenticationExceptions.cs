namespace SalesManagement.Core.Exceptions;
public sealed class AuthenticationFailedException() : Exception("아이디 또는 비밀번호가 올바르지 않습니다.");
public sealed class AccountLockedException(DateTimeOffset lockedUntil) : Exception($"계정이 {lockedUntil:u}까지 잠겼습니다.") { public DateTimeOffset LockedUntil { get; } = lockedUntil; }
