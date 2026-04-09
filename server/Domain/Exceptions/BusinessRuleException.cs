namespace Domain.Exceptions;

public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }
}

public class MembershipException : BusinessRuleException
{
    public MembershipException(string message) : base(message) { }
}
