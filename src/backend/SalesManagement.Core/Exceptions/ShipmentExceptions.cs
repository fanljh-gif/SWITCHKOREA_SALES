namespace SalesManagement.Core.Exceptions;
public sealed class MissingMonthlyRateException() : Exception("해당 월의 지정환율이 없습니다.");
public sealed class MissingEffectivePriceException() : Exception("출고일에 유효한 단가가 없습니다.");
public sealed class DuplicateShipmentNumberException() : Exception("같은 거래처에 동일한 출고번호가 이미 있습니다.");
