namespace MediDropProject.Domain.Enums;

public enum SecurityEventType
{
    Login,
    Logout,
    FailedLogin,
    PasswordChanged,
    PasswordReset,
    AccountLocked,
    AccountUnlocked,
    MfaEnabled,
    MfaDisabled,
    MfaFailed,
    SessionRevoked,
    CredentialChanged,
    SuspiciousActivity
}