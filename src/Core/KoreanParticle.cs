namespace PokeTokenBar.Core;

/// <summary>한국어 조사 종류 — 주격(이/가), 목적격(을/를), 방향((으)로).</summary>
public enum KoreanParticle
{
    Subject,
    Object,
    Direction
}

/// <summary>
/// 한국어 조사 선택 — 런타임에 오는 이름(PokéAPI) 뒤에 받침에 맞는 조사를 붙인다.
/// 번역문에 "이(가)"처럼 병기하던 자리를 "피카츄가", "이상해씨가"처럼 자연스럽게
/// 만든다. macOS KoreanParticle.swift (#356) 포팅.
///
/// 판정은 마지막 글자가 한글 음절(U+AC00…U+D7A3)일 때만 한다. 그 밖(안농의 "[A]",
/// 이름 로딩 전 "#123" 등)은 받침을 알 수 없으므로 기존 병기 형태를 그대로
/// 붙인다 — 틀린 조사보다 병기가 낫다.
/// </summary>
public static class KoreanParticles
{
    public static string Attach(KoreanParticle particle, string word)
    {
        if (Coda(word) is not { } coda) return word + Fallback(particle);
        return particle switch
        {
            KoreanParticle.Subject => word + (coda == 0 ? "가" : "이"),
            KoreanParticle.Object => word + (coda == 0 ? "를" : "을"),
            // ㄹ 받침은 "으로"가 아니라 "로"(서울로·알로).
            KoreanParticle.Direction => word + (coda == 0 || coda == Rieul ? "로" : "으로"),
            _ => word + Fallback(particle)
        };
    }

    private static string Fallback(KoreanParticle particle) => particle switch
    {
        KoreanParticle.Subject => "이(가)",
        KoreanParticle.Object => "을(를)",
        _ => "(으)로"
    };

    private const char SyllableFirst = '\uAC00';
    private const char SyllableLast = '\uD7A3';
    private const int Rieul = 8;

    /// <summary>마지막 글자의 종성 인덱스(0 = 받침 없음). 한글 음절이 아니면 null.</summary>
    private static int? Coda(string word)
    {
        if (word.Length == 0) return null;
        var last = word[^1];
        if (last < SyllableFirst || last > SyllableLast) return null;
        return (last - SyllableFirst) % 28;
    }
}
