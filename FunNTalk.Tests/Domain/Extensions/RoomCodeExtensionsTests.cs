using FunNTalk.Domain.Extensions;

namespace FunNTalk.Tests.Domain.Extensions;

[TestClass]
public class RoomCodeExtensionsTests
{
    [TestMethod]
    public void NormalizeRoomCode_TrimsLeadingAndTrailingWhitespace()
    {
        Assert.AreEqual("A4K9X2", " A4K9X2 ".NormalizeRoomCode());
    }

    [TestMethod]
    public void NormalizeRoomCode_UppercasesLowercaseInput()
    {
        Assert.AreEqual("A4K9X2", "a4k9x2".NormalizeRoomCode());
    }

    [TestMethod]
    public void NormalizeRoomCode_FoldsIAndLowercaseLToOne()
    {
        Assert.AreEqual("111111", "IlIlIl".NormalizeRoomCode());
    }

    [TestMethod]
    public void NormalizeRoomCode_FoldsLowercaseOToZero()
    {
        Assert.AreEqual("000000", "oooooo".NormalizeRoomCode());
    }

    [TestMethod]
    public void NormalizeRoomCode_IsIdempotent()
    {
        var once = " a4k9x2 ".NormalizeRoomCode();
        var twice = once.NormalizeRoomCode();

        Assert.AreEqual(once, twice);
    }

    [TestMethod]
    public void NormalizeRoomCode_ReturnsEmptyForNull()
    {
        string? code = null;

        Assert.AreEqual(string.Empty, code.NormalizeRoomCode());
    }

    [TestMethod]
    public void NormalizeRoomCode_ReturnsEmptyForEmptyString()
    {
        Assert.AreEqual(string.Empty, "".NormalizeRoomCode());
    }

    [TestMethod]
    public void IsWellFormedRoomCode_AcceptsAValidSixCharacterCrockfordCode()
    {
        Assert.IsTrue("A4K9X2".IsWellFormedRoomCode());
    }

    [TestMethod]
    public void IsWellFormedRoomCode_RejectsTheWrongLength()
    {
        Assert.IsFalse("A4K9X".IsWellFormedRoomCode());
        Assert.IsFalse("A4K9X22".IsWellFormedRoomCode());
    }

    [TestMethod]
    public void IsWellFormedRoomCode_RejectsExcludedCrockfordCharacters()
    {
        Assert.IsFalse("A4K9XI".IsWellFormedRoomCode());
        Assert.IsFalse("A4K9XL".IsWellFormedRoomCode());
        Assert.IsFalse("A4K9XO".IsWellFormedRoomCode());
        Assert.IsFalse("A4K9XU".IsWellFormedRoomCode());
    }

    [TestMethod]
    public void IsWellFormedRoomCode_RejectsANonAlphabetSymbol()
    {
        Assert.IsFalse("A4K9X!".IsWellFormedRoomCode());
    }
}
