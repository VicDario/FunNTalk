using FunNTalk.Infrastructure.Configuration;

namespace FunNTalk.Tests.Infrastructure.Configuration;

[TestClass]
public class RoomOptionsTests
{
    [TestMethod]
    public void Validate_AcceptsTheDefaults()
    {
        Assert.IsTrue(RoomOptions.Validate(new RoomOptions()));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Validate_RejectsANonPositiveVacancyTtl(int vacancyTtlMinutes)
    {
        Assert.IsFalse(RoomOptions.Validate(new RoomOptions { VacancyTtlMinutes = vacancyTtlMinutes }));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Validate_RejectsANonPositiveSweepInterval(int sweepIntervalSeconds)
    {
        Assert.IsFalse(RoomOptions.Validate(new RoomOptions { SweepIntervalSeconds = sweepIntervalSeconds }));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Validate_RejectsAMaxMintAttemptsBelowOne(int maxMintAttempts)
    {
        Assert.IsFalse(RoomOptions.Validate(new RoomOptions { MaxMintAttempts = maxMintAttempts }));
    }
}
