using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class UnitTypeTests
{
    [TestMethod]
    public void Unit_Value_IsDefaultInstance()
    {
        Unit a = Unit.Value;
        Unit b = new Unit();

        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void Unit_Equals_AnotherUnit()
    {
        Assert.AreEqual(Unit.Value, Unit.Value);
        Assert.IsTrue(Unit.Value.Equals(Unit.Value));
        Assert.IsTrue(Unit.Value.Equals((object)Unit.Value));
    }

    [TestMethod]
    public void Unit_ToString_ReturnsEmptyTuple()
    {
        Assert.AreEqual("()", Unit.Value.ToString());
    }

    [TestMethod]
    public void Unit_GetHashCode_IsZero()
    {
        Assert.AreEqual(0, Unit.Value.GetHashCode());
    }

    [TestMethod]
    public void Unit_NotEqualToNull()
    {
        Assert.IsFalse(Unit.Value.Equals(null));
    }

    [TestMethod]
    public void Unit_NotEqualToOtherType()
    {
        Assert.IsFalse(Unit.Value.Equals("not a unit"));
    }
}
