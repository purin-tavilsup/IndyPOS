namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// A caller without reports.view asked for a day other than today. Mapped to 403 — the day's
/// existence is not a secret, only its figures are. (A single other-day BILL is 404 instead, so an
/// old bill number's existence is not revealed.)
/// </summary>
public class OtherDayForbiddenException()
    : Exception("ดูได้เฉพาะบิลและยอดของวันนี้เท่านั้น");
