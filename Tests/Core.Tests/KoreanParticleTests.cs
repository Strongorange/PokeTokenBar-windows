using PokeTokenBar.Core;

namespace PokeTokenBar.Core.Tests;

/// <summary>
/// 한국어 조사 선택 — 이름의 마지막 음절 받침으로 고르고, 판정할 수 없으면
/// 병기 형태를 유지한다. macOS KoreanParticleTests.swift 포팅 (#356).
/// </summary>
public class KoreanParticleTests
{
    [Fact]
    public void SubjectFollowsFinalConsonant()
    {
        Assert.Equal("유니란이", KoreanParticles.Attach(KoreanParticle.Subject, "유니란"));
        Assert.Equal("피카츄가", KoreanParticles.Attach(KoreanParticle.Subject, "피카츄"));
    }

    [Fact]
    public void ObjectFollowsFinalConsonant()
    {
        Assert.Equal("꼬부기를", KoreanParticles.Attach(KoreanParticle.Object, "꼬부기"));
        Assert.Equal("리자몽을", KoreanParticles.Attach(KoreanParticle.Object, "리자몽"));
    }

    /// "으로/로"는 받침 없음과 ㄹ 받침이 같은 쪽이다 — 이 분기가 빠지면 "알으로"가 된다.
    [Fact]
    public void DirectionTreatsRieulLikeNoFinalConsonant()
    {
        Assert.Equal("란쿨루스로", KoreanParticles.Attach(KoreanParticle.Direction, "란쿨루스"));
        Assert.Equal("듀란으로", KoreanParticles.Attach(KoreanParticle.Direction, "듀란"));
        Assert.Equal("희귀 알로", KoreanParticles.Attach(KoreanParticle.Direction, "희귀 알"));
        Assert.Equal("레트라로", KoreanParticles.Attach(KoreanParticle.Direction, "레트라"));
    }

    /// 받침을 알 수 없는 끝(안농 글자, 이름 로딩 전 번호, 라틴 문자, 빈 문자열)은 병기 형태 그대로.
    [Fact]
    public void NonHangulEndingKeepsBothForms()
    {
        Assert.Equal("안농 [A]이(가)", KoreanParticles.Attach(KoreanParticle.Subject, "안농 [A]"));
        Assert.Equal("#123을(를)", KoreanParticles.Attach(KoreanParticle.Object, "#123"));
        Assert.Equal("Pikachu(으)로", KoreanParticles.Attach(KoreanParticle.Direction, "Pikachu"));
        Assert.Equal("이(가)", KoreanParticles.Attach(KoreanParticle.Subject, ""));
    }

    /// 실제 문구 — "유니란이(가)"·"듀란(으)로"·"란쿨루스(으)로" 같은 병기가 사라진다.
    [Fact]
    public void KoreanCopyUsesTheMatchingParticle()
    {
        Assert.Equal("알에서 유니란이 나왔어요!",
            DashboardText.EventHatch(AppLanguage.Ko, "유니란", false, 64));
        Assert.Equal("이로치 뚜벅쵸가 알에서 태어났어요! (1/48)",
            DashboardText.EventHatch(AppLanguage.Ko, "뚜벅쵸", true, 48));
        Assert.Equal("듀란으로 진화했어요!", DashboardText.EventEvolve(AppLanguage.Ko, "듀란"));
        Assert.Equal("란쿨루스로 진화했어요!",
            DashboardText.StatusEvolved(AppLanguage.Ko, "란쿨루스"));
        Assert.Equal("레트라를 놓아주고 희귀 알로 바꿀까요?",
            DashboardText.EggConfirm(AppLanguage.Ko, "레트라",
                DashboardText.EggName(AppLanguage.Ko, Rarity.Rare)));
        Assert.Equal("민트 사용 — 성격이 명랑으로 변했어요",
            DashboardText.MintUsed(AppLanguage.Ko,
                PokemonNatures.Name(PokemonNature.Jolly, AppLanguage.Ko)));
    }

    /// 조사 선택은 한국어 문구에만 적용된다.
    [Fact]
    public void OtherLanguagesAreUntouched()
    {
        Assert.Equal("Unfezant hatched from the egg!",
            DashboardText.EventHatch(AppLanguage.En, "Unfezant", false, 64));
        Assert.Equal("デスカーン に進化しました！",
            DashboardText.EventEvolve(AppLanguage.Ja, "デスカーン"));
    }
}
