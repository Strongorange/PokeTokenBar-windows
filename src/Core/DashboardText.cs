namespace PokeTokenBar.Core;

/// <summary>
/// Fixed UI strings for the Windows dashboard, tray menu, detail window and
/// engine-emitted event texts, in the seven app languages. Values are copied
/// from the macOS original (Localization.swift) where the same concept exists
/// so both apps read identically; Windows-only strings follow the same tone.
/// </summary>
public static class DashboardText
{
    public static string T(AppLanguage lang,
        string ko, string en, string ja, string es, string fr, string pt, string de) => lang switch
    {
        AppLanguage.Ko => ko,
        AppLanguage.Ja => ja,
        AppLanguage.Es => es,
        AppLanguage.Fr => fr,
        AppLanguage.Pt => pt,
        AppLanguage.De => de,
        _ => en
    };

    // Tray menu

    public static string OpenDashboard(AppLanguage lang) =>
        T(lang, "대시보드 열기", "Open dashboard", "ダッシュボードを開く", "Abrir panel",
            "Ouvrir le tableau de bord", "Abrir painel", "Dashboard öffnen");

    public static string OpenDiagnosticsFolder(AppLanguage lang) =>
        T(lang, "진단 폴더 열기", "Open diagnostics folder", "診断フォルダを開く",
            "Abrir carpeta de diagnóstico", "Ouvrir le dossier de diagnostic",
            "Abrir pasta de diagnóstico", "Diagnoseordner öffnen");

    public static string ExitApp(AppLanguage lang) =>
        T(lang, "종료", "Exit", "終了", "Salir", "Quitter", "Sair", "Beenden");

    // Usage tab

    public static string UsageTab(AppLanguage lang) =>
        T(lang, "사용량", "Usage", "使用量", "Uso", "Usage", "Uso", "Verbrauch");

    public static string ProvidersTitle(AppLanguage lang) =>
        T(lang, "프로바이더", "Providers", "プロバイダー", "Proveedores", "Fournisseurs",
            "Provedores", "Anbieter");

    public static string CombinedTitle(AppLanguage lang) =>
        T(lang, "합계", "Combined", "合計", "Total", "Total", "Total", "Gesamt");

    public static string TodayLabel(AppLanguage lang) =>
        T(lang, "오늘", "Today", "今日", "Hoy", "Aujourd'hui", "Hoje", "Heute");

    public static string MonthLabel(AppLanguage lang) =>
        T(lang, "이 달", "Month", "今月", "Mes", "Mois", "Mês", "Monat");

    public static string DailyTrend(AppLanguage lang) =>
        T(lang, "이번 달 일별", "Daily this month", "今月の日別",
            "Diario de este mes", "Par jour ce mois-ci", "Diário deste mês", "Täglich");

    public static string PeakDay(AppLanguage lang) =>
        T(lang, "최다", "Peak", "最多", "Máx.", "Max.", "Máx.", "Max.");

    public static string RefreshButton(AppLanguage lang) =>
        T(lang, "새로 고침", "Refresh", "更新", "Actualizar", "Actualiser", "Atualizar",
            "Aktualisieren");

    public static string Refreshing(AppLanguage lang) =>
        T(lang, "새로 고침 중…", "Refreshing…", "更新中…", "Actualizando…", "Actualisation…",
            "Atualizando…", "Aktualisieren…");

    public static string RefreshedAt(AppLanguage lang, string time) =>
        T(lang, $"갱신 {time}", $"Refreshed {time}", $"更新 {time}", $"Actualizado {time}",
            $"Actualisé {time}", $"Atualizado {time}", $"Aktualisiert {time}");

    public static string ProviderNotFound(AppLanguage lang) =>
        T(lang, "(없음)", "(not found)", "(見つかりません)", "(no encontrado)", "(introuvable)",
            "(não encontrado)", "(nicht gefunden)");

    // Game tab

    public static string GameTab(AppLanguage lang) =>
        T(lang, "게임", "Game", "ゲーム", "Juego", "Jeu", "Jogo", "Spiel");

    public static string TokenEgg(AppLanguage lang) =>
        T(lang, "Token Egg", "Token Egg", "Token Egg", "Token Egg", "Token Egg", "Token Egg",
            "Token Egg");

    public static string EggHint(AppLanguage lang) =>
        T(lang, "AI 도구를 계속 쓰면 알이 부화해요.",
            "Keep using your AI tools — the egg is incubating.",
            "AIツールを使い続けるとタマゴが孵化します。",
            "Sigue usando tus herramientas de IA: el huevo está incubando.",
            "Continue d'utiliser tes outils IA — l'œuf est en incubation.",
            "Continue usando suas ferramentas de IA — o ovo está chocando.",
            "Nutze deine KI-Tools weiter — das Ei wird ausgebrütet.");

    public static string StageLabel(AppLanguage lang, int stage, int total) =>
        T(lang, $"단계 {stage}/{total}", $"Stage {stage}/{total}", $"段階 {stage}/{total}",
            $"Etapa {stage}/{total}", $"Étape {stage}/{total}", $"Estágio {stage}/{total}",
            $"Stufe {stage}/{total}");

    public static string TokensUnit(AppLanguage lang) =>
        T(lang, "토큰", "tokens", "トークン", "tokens", "jetons", "tokens", "Tokens");

    public static string DexTitle(AppLanguage lang) =>
        T(lang, "도감", "Dex", "図鑑", "Pokédex", "Pokédex", "Pokédex", "Pokédex");

    public static string DexSpeciesCount(AppLanguage lang, int count) =>
        T(lang, $"{count}종", $"{count} species", $"{count}種", $"{count} especies",
            $"{count} espèces", $"{count} espécies", $"{count} Spezies");

    public static string WalletLabel(AppLanguage lang) =>
        T(lang, "지갑", "wallet", "ウォレット", "cartera", "bourse", "carteira", "Geldbeutel");

    public static string DetailHint(AppLanguage lang) =>
        T(lang, "상세는 더블클릭", "double-click for details", "詳細はダブルクリック",
            "detalles con doble clic", "détails par double-clic", "detalhes com duplo clique",
            "Details per Doppelklick");

    public static string UnownFormsCollected(AppLanguage lang, int count) =>
        T(lang, $"안농 글자 {count}/28", $"Unown forms {count}/28", $"アンノーン {count}/28文字",
            $"Formas Unown {count}/28", $"Formes Zarbi {count}/28", $"Formas Unown {count}/28",
            $"Icognito-Formen {count}/28");

    public static string UnownNotCollected(AppLanguage lang) =>
        T(lang, "미수집", "Not collected", "未収集", "Sin conseguir", "Non collectionnée",
            "Não coletada", "Noch nicht gesammelt");

    public static string ShopTitle(AppLanguage lang) =>
        T(lang, "상점", "Shop", "ショップ", "Tienda", "Boutique", "Loja", "Laden");

    public static string CompanionEventsLabel(AppLanguage lang) =>
        T(lang, "컴패니언 이벤트 (부화·진화·졸업)", "Companion events (hatch / evolve / graduate)",
            "コンパニオンイベント（孵化・進化・卒業）",
            "Eventos del compañero (eclosión / evolución / graduación)",
            "Événements du compagnon (éclosion / évolution / diplôme)",
            "Eventos do companheiro (nascimento / evolução / formatura)",
            "Begleiter-Ereignisse (Schlüpfen / Entwicklung / Abschied)");

    public static string BuySelected(AppLanguage lang) =>
        T(lang, "선택 구매", "Buy selected", "選択項目を購入", "Comprar selección",
            "Acheter la sélection", "Comprar seleção", "Auswahl kaufen");

    public static string UseItem(AppLanguage lang, string itemName) =>
        T(lang, $"{itemName} 사용", $"Use {itemName}", $"{itemName}を使う", $"Usar {itemName}",
            $"Utiliser {itemName}", $"Usar {itemName}", $"{itemName} einsetzen");

    public static string UseAll(AppLanguage lang) =>
        T(lang, "전부 사용", "Use all", "すべて使う", "Usar todo", "Tout utiliser", "Usar tudo",
            "Alle verwenden");

    public static string ExportSave(AppLanguage lang) =>
        T(lang, "내보내기…", "Export…", "エクスポート…", "Exportar…", "Exporter…", "Exportar…",
            "Exportieren…");

    public static string ImportSave(AppLanguage lang) =>
        T(lang, "가져오기…", "Import…", "インポート…", "Importar…", "Importer…", "Importar…",
            "Importieren…");

    // Settings window

    public static string SettingsTitle(AppLanguage lang) =>
        T(lang, "설정", "Settings", "設定", "Ajustes", "Réglages", "Ajustes", "Einstellungen");

    public static string LanguageLabel(AppLanguage lang) =>
        T(lang, "언어", "Language", "言語", "Idioma", "Langue", "Idioma", "Sprache");

    public static string GeneralSectionTitle(AppLanguage lang) =>
        T(lang, "일반", "General", "一般", "General", "Général", "Geral", "Allgemein");

    public static string DifficultySection(AppLanguage lang) =>
        T(lang, "난이도", "Difficulty", "難易度", "Dificultad", "Difficulté", "Dificuldade",
            "Schwierigkeit");

    public static string DifficultyShopLabel(AppLanguage lang) =>
        T(lang, "상점 가격", "Shop prices", "ショップ価格", "Precios de la tienda",
            "Prix de la boutique", "Preços da loja", "Shop-Preise");

    public static string DifficultyHint(AppLanguage lang) =>
        T(lang, "기본값 100% 기준이에요 — 낮추면 빨리 자라고 싸지고, 높이면 그 반대예요",
            "Percentages of the default balance — lower grows faster and costs less, higher does the opposite",
            "標準バランスに対する割合です — 下げると早く育ち安くなり、上げるとその逆になります",
            "Porcentajes del balance predeterminado: si los bajas, crece más rápido y cuesta menos; si los subes, al revés",
            "Pourcentages de l'équilibrage par défaut — plus bas, la croissance est plus rapide et les prix baissent ; plus haut, l'inverse",
            "Porcentagens do balanceamento padrão — reduzir faz crescer mais rápido e custar menos; aumentar faz o contrário",
            "Prozentwerte der Standardbalance — niedriger wächst schneller und kostet weniger, höher bewirkt das Gegenteil");

    public static string ScanFoldersTitle(AppLanguage lang) =>
        T(lang, "추가 스캔 폴더", "Additional scan folders", "追加スキャンフォルダ",
            "Carpetas de escaneo adicionales", "Dossiers d'analyse supplémentaires",
            "Pastas extras para escanear", "Zusätzliche Scan-Ordner");

    public static string ScanFoldersHint(AppLanguage lang) =>
        T(lang, "선택한 프로바이더의 로그가 기본 위치 밖에 있을 때만 추가하세요. 다른 프로바이더의 폴더를 넣지 마세요.",
            "Only add this provider's log folders outside the built-in locations. Do not point at another provider's folder.",
            "選択したプロバイダーのログが既定の場所にないときだけ追加してください。別のプロバイダーのフォルダは指定しないでください。",
            "Añade solo carpetas de registros de este proveedor fuera de las ubicaciones integradas. No indiques la carpeta de otro proveedor.",
            "N'ajoutez que les dossiers de journaux de ce fournisseur hors des emplacements intégrés. N'indiquez pas le dossier d'un autre fournisseur.",
            "Adicione apenas pastas de logs deste provedor fora dos locais padrão. Não aponte para a pasta de outro provedor.",
            "Füge nur Protokollordner dieses Anbieters außerhalb der Standardpfade hinzu. Wähle keinen Ordner eines anderen Anbieters.");

    public static string AddFolderButton(AppLanguage lang) =>
        T(lang, "추가…", "Add…", "追加…", "Añadir…", "Ajouter…", "Adicionar…", "Hinzufügen…");

    public static string RemoveButton(AppLanguage lang) =>
        T(lang, "제거", "Remove", "削除", "Quitar", "Retirer", "Remover", "Entfernen");

    public static string GrowthLabel(AppLanguage lang) =>
        T(lang, "성장", "Growth", "成長", "Crecimiento", "Croissance", "Crescimento",
            "Wachstum");

    public static string GrowthBoostMark(AppLanguage lang, int multiplier) =>
        T(lang, $"{multiplier}× 성장", $"{multiplier}× growth", $"成長 {multiplier}倍",
            $"Crecimiento ×{multiplier}", $"Croissance ×{multiplier}",
            $"Crescimento ×{multiplier}", $"{multiplier}× Wachstum");

    public static string FloatingPet(AppLanguage lang) =>
        T(lang, "플로팅 펫", "Floating pet", "フローティングペット", "Mascota flotante",
            "Compagnon flottant", "Mascote flutuante", "Schwebendes Pet");

    public static string HidePet(AppLanguage lang) =>
        T(lang, "펫 숨기기", "Hide pet", "ペットを隠す", "Ocultar mascota",
            "Masquer le compagnon", "Ocultar mascote", "Pet ausblenden");

    public static string SizeLabel(AppLanguage lang) =>
        T(lang, "크기", "Size", "サイズ", "Tamaño", "Taille", "Tamanho", "Größe");

    public static string BagLabel(AppLanguage lang) =>
        T(lang, "가방", "Bag", "バッグ", "Bolsa", "Sac", "Bolsa", "Beutel");

    public static string BagEmpty(AppLanguage lang) =>
        T(lang, "비어 있음", "empty", "空き", "vacío", "vide", "vazio", "leer");

    public static string LifetimeLabel(AppLanguage lang) =>
        T(lang, "누적", "Lifetime", "累計", "Acumulado", "Cumulé", "Total", "Lebenszeit");

    public static string RaisingLabel(AppLanguage lang) =>
        T(lang, "키우는 중", "raising", "育成中", "criando", "en élevage", "treinando",
            "in Aufzucht");

    public static string ShinyLabel(AppLanguage lang) =>
        T(lang, "이로치", "Shiny", "色違い", "Variocolor", "Chromatique", "Shiny", "Schillernd");

    public static string FinalForm(AppLanguage lang) =>
        T(lang, "최종 진화체", "Final form", "最終進化", "Forma final", "Forme finale", "Forma final",
            "Letzte Entwicklungsstufe");

    public static string UnknownNextEvolution(AppLanguage lang) =>
        T(lang, "알 수 없는 다음 진화", "Unknown next evolution", "次の進化先は不明",
            "Próxima evolución desconocida", "Prochaine évolution inconnue",
            "Próxima evolución desconhecida", "Nächste Entwicklung unbekannt");

    public static string RarityLabel(AppLanguage lang, Rarity rarity) => rarity switch
    {
        Rarity.Common => T(lang, "일반", "Common", "ノーマル", "Común", "Commun", "Comum",
            "Gewöhnlich"),
        Rarity.Uncommon => T(lang, "고급", "Uncommon", "アンコモン", "Poco común", "Peu commun",
            "Incomum", "Ungewöhnlich"),
        Rarity.Rare => T(lang, "희귀", "Rare", "レア", "Raro", "Rare", "Raro", "Selten"),
        Rarity.Legendary => T(lang, "전설", "Legendary", "伝説", "Legendario", "Légendaire",
            "Lendário", "Legendär"),
        _ => ""
    };

    public static string ItemName(AppLanguage lang, ItemKind kind) => kind switch
    {
        ItemKind.RareCandy => T(lang, "이상한 사탕", "Rare Candy", "ふしぎなアメ", "Caramelo Raro",
            "Super Bonbon", "Doce Raro", "Sonderbonbon"),
        ItemKind.Mint => T(lang, "민트", "Mint", "ミント", "Menta", "Menthe", "Menta", "Minze"),
        ItemKind.ShinyCharm => T(lang, "이로치 부적", "Shiny Charm", "ひかるおまもり",
            "Amuleto Iris", "Charme Chroma", "Amuleto Shiny", "Schillerpin"),
        _ => kind.ToString()
    };

    public static string EggName(AppLanguage lang, Rarity? tier) => tier switch
    {
        null => T(lang, "포켓몬 알", "Pokémon Egg", "ポケモンのタマゴ", "Huevo Pokémon",
            "Œuf Pokémon", "Ovo Pokémon", "Pokémon-Ei"),
        Rarity.Uncommon => T(lang, "고급 알", "Uncommon Egg", "アンコモンのタマゴ",
            "Huevo poco común", "Œuf peu commun", "Ovo incomum", "Ungewöhnliches Ei"),
        Rarity.Rare => T(lang, "희귀 알", "Rare Egg", "レアのタマゴ", "Huevo raro", "Œuf rare",
            "Ovo raro", "Seltenes Ei"),
        Rarity.Legendary => T(lang, "전설 알", "Legendary Egg", "でんせつのタマゴ",
            "Huevo legendario", "Œuf légendaire", "Ovo lendário", "Legendäres Ei"),
        _ => T(lang, "포켓몬 알", "Pokémon Egg", "ポケモンのタマゴ", "Huevo Pokémon",
            "Œuf Pokémon", "Ovo Pokémon", "Pokémon-Ei")
    };

    public static string BagTitle(AppLanguage lang) =>
        T(lang, "가방", "Bag", "バッグ", "Bolsa", "Sac", "Bolsa", "Beutel");

    public static string SpendableTokens(AppLanguage lang) =>
        T(lang, "쓸 수 있는 토큰", "Spendable tokens", "使えるトークン", "Tokens disponibles",
            "Tokens disponibles", "Tokens disponíveis", "Verfügbare Tokens");

    public static string ShopHint(AppLanguage lang) =>
        T(lang, "사용한 토큰으로 아이템을 살 수 있어요.", "Spend the tokens you've used on items.",
            "使ったトークンでアイテムを購入できます。", "Usa los tokens que has consumido para comprar objetos.",
            "Dépense les tokens que tu as consommés pour acheter des objets.",
            "Compre itens com os tokens que você já usou.",
            "Mit deinen verbrauchten Tokens kannst du Gegenstände kaufen.");

    public static string BuyLabel(AppLanguage lang) =>
        T(lang, "구매", "Buy", "購入", "Comprar", "Acheter", "Comprar", "Kaufen");

    public static string BuyConfirm(AppLanguage lang, string name) =>
        T(lang, $"{name} 구매할까요?", $"Buy {name}?", $"{name} を購入しますか？",
            $"¿Comprar {name}?", $"Acheter {name} ?", $"Comprar {name}?", $"{name} kaufen?");

    public static string CancelLabel(AppLanguage lang) =>
        T(lang, "취소", "Cancel", "キャンセル", "Cancelar", "Annuler", "Cancelar", "Abbrechen");

    public static string NotEnoughTokens(AppLanguage lang) =>
        T(lang, "토큰이 부족해요", "Not enough tokens", "トークンが足りません",
            "No tienes suficientes tokens", "Pas assez de tokens", "Tokens insuficientes",
            "Nicht genug Tokens");

    public static string OwnedCount(AppLanguage lang, int count) =>
        T(lang, $"보유 ×{count}", $"Owned ×{count}", $"所持 ×{count}", $"En posesión ×{count}",
            $"Possédés ×{count}", $"Você tem ×{count}", $"Im Beutel ×{count}");

    public static string ShopPriceLabel(AppLanguage lang) =>
        T(lang, "가격", "Price", "価格", "Precio", "Prix", "Preço", "Preis");

    public static string OwnedAlready(AppLanguage lang) =>
        T(lang, "보유 중", "Owned", "所持済み", "En posesión", "Possédé", "Já tem", "Im Beutel");

    public static string ItemDescription(AppLanguage lang, ItemKind kind)
    {
        var xp = TokenFormatter.Compact(RareCandies.Xp);
        return kind switch
        {
            ItemKind.RareCandy => T(lang, $"현재 포켓몬의 경험치를 {xp} 올려줘요.",
                $"Raises your Pokémon's EXP by {xp}.", $"ポケモンの経験値を{xp}上げます。",
                $"Aumenta la experiencia de tu Pokémon en {xp}.",
                $"Augmente l'EXP de ton Pokémon de {xp}.",
                $"Aumenta a experiência do seu Pokémon em {xp}.",
                $"Gibt deinem aktuellen Pokémon {xp} EP."),
            ItemKind.Mint => T(lang, "현재 포켓몬의 성격을 랜덤으로 바꿔줘요.",
                "Randomly changes your Pokémon's nature.", "ポケモンのせいかくをランダムに変えます。",
                "Cambia aleatoriamente la naturaleza de tu Pokémon.",
                "Change aléatoirement la nature de ton Pokémon.",
                "Muda a natureza do seu Pokémon aleatoriamente.",
                "Ändert das Wesen deines aktuellen Pokémon zufällig."),
            ItemKind.ShinyCharm => T(lang, "보유하면 이로치 포켓몬이 태어날 확률이 올라가요.",
                "While owned, raises the chance of hatching a shiny.",
                "持っていると色違いが生まれる確率が上がります。",
                "Mientras lo tengas, aumenta la probabilidad de que nazca un Pokémon variocolor.",
                "Tant que tu le possèdes, augmente les chances qu'un Pokémon chromatique éclose.",
                "Enquanto estiver na sua bolsa, aumenta a chance de nascer um Pokémon shiny.",
                "Erhöht im Beutel die Chance, dass ein schillerndes Pokémon schlüpft."),
            _ => ""
        };
    }

    public static string EggDescription(AppLanguage lang, Rarity? tier)
    {
        if (tier is null || tier == Rarity.Common)
            return T(lang, "지금 포켓몬을 놓아주고 새 알로 다시 시작해요.",
                "Send off your current Pokémon and start fresh with a new egg.",
                "いまのポケモンを手放して新しいタマゴからやり直します。",
                "Suelta a tu Pokémon actual y empieza de nuevo con un huevo nuevo.",
                "Laisse partir ton Pokémon actuel et repars de zéro avec un nouvel œuf.",
                "Solte seu Pokémon atual e recomece com um ovo novo.",
                "Verabschiede dein aktuelles Pokémon und starte mit einem neuen Ei.");
        var rarity = RarityLabel(lang, tier.Value);
        return T(lang, $"지금 포켓몬을 놓아주고 {rarity} 이상이 확정으로 나오는 알을 받아요.",
            $"Send off your current Pokémon for an egg guaranteed to hatch {rarity} or better.",
            $"いまのポケモンを手放して {rarity} 以上が確定で孵るタマゴをもらいます。",
            $"Suelta a tu Pokémon actual y consigue un huevo garantizado de {rarity} o superior.",
            $"Laisse partir ton Pokémon actuel pour un œuf garanti {rarity} ou mieux.",
            $"Solte seu Pokémon atual e ganhe um ovo que garante {rarity} ou melhor.",
            $"Verabschiede dein aktuelles Pokémon und erhalte ein Ei, aus dem garantiert ein Pokémon der Seltenheitsstufe {rarity} oder höher schlüpft.");
    }

    public static string EggShopLockedHint(AppLanguage lang) =>
        T(lang, "지금 품고 있는 알이 부화하면 살 수 있어요.",
            "Available once your current egg hatches.",
            "いま抱えているタマゴが孵ると購入できます。",
            "Disponible cuando eclosione tu huevo actual.",
            "Disponible une fois ton œuf actuel éclos.",
            "Disponível quando seu ovo atual chocar.",
            "Verfügbar, sobald dein aktuelles Ei geschlüpft ist.");

    public static string EggConfirm(AppLanguage lang, string monName, string eggName) =>
        T(lang, $"{monName}을(를) 놓아주고 {eggName}(으)로 바꿀까요?",
            $"Send off {monName} for the {eggName}?",
            $"{monName} を手放して {eggName} にしますか？",
            $"¿Soltar a {monName} y cambiarlo por {eggName}?",
            $"Laisser partir {monName} pour le {eggName} ?",
            $"Soltar {monName} e trocar pelo {eggName}?",
            $"{monName} verabschieden und gegen {eggName} tauschen?");

    public static string FreshEggShinyWarning(AppLanguage lang) =>
        T(lang, "⚠️ 이로치 포켓몬이에요! 정말 놓아줄까요?", "⚠️ This one is shiny! Really send it off?",
            "⚠️ 色違いです！本当に手放しますか？", "⚠️ ¡Este es variocolor! ¿Seguro que quieres soltarlo?",
            "⚠️ Celui-ci est chromatique ! Vraiment le laisser partir ?",
            "⚠️ Esse é shiny! Quer mesmo soltar?",
            "⚠️ Dieses Pokémon ist schillernd! Wirklich verabschieden?");

    public static string FreshEggDiscardShiny(AppLanguage lang) =>
        T(lang, "이로치 놓아주기", "Send shiny off", "手放す", "Soltar variocolor",
            "Laisser partir le chromatique", "Soltar o shiny",
            "Schillerndes Pokémon verabschieden");

    public static string EggIncubating(AppLanguage lang) =>
        T(lang, "🥚 부화 준비 중", "🥚 Incubating", "🥚 孵化の準備中", "🥚 Incubando",
            "🥚 En incubation", "🥚 Incubando", "🥚 Wird ausgebrütet");

    public static string EggImminent(AppLanguage lang) =>
        T(lang, "곧 부화해요!", "About to hatch!", "もうすぐ孵化！", "¡Está a punto de eclosionar!",
            "Sur le point d'éclore !", "Está quase chocando!", "Schlüpft gleich!");

    public static string EggToHatch(AppLanguage lang, string amount) =>
        T(lang, $"부화까지 {amount}", $"{amount} to hatch", $"孵化まで {amount}",
            $"{amount} para eclosionar", $"{amount} avant l'éclosion", $"{amount} para chocar",
            $"{amount} bis zum Schlüpfen");

    public static string ToNextEvolution(AppLanguage lang, string amount) =>
        T(lang, $"다음 진화까지 {amount}", $"{amount} to next evolution", $"次の進化まで {amount}",
            $"{amount} para la siguiente evolución", $"{amount} avant la prochaine évolution",
            $"{amount} para a próxima evolução", $"{amount} bis zur nächsten Entwicklung");

    public static string ToGraduation(AppLanguage lang, string amount) =>
        T(lang, $"졸업까지 {amount}", $"{amount} to graduation", $"卒業まで {amount}",
            $"{amount} para graduarse", $"{amount} avant le diplôme", $"{amount} para se formar",
            $"{amount} bis zum Abschied");

    public static string GrowthBoost(AppLanguage lang, int multiplier) =>
        T(lang, $"{multiplier}× 성장", $"{multiplier}× growth", $"成長 {multiplier}倍",
            $"Crecimiento ×{multiplier}", $"Croissance ×{multiplier}", $"Crescimento ×{multiplier}",
            $"{multiplier}× Wachstum");

    public static string EggGuaranteeHint(AppLanguage lang, Rarity tier)
    {
        var rarity = RarityLabel(lang, tier);
        return T(lang, $"{rarity} 이상 확정", $"{rarity} or better", $"{rarity} 以上確定",
            $"{rarity} o superior garantizado", $"{rarity} ou mieux garanti",
            $"{rarity} ou melhor garantido", $"Garantiert {rarity} oder besser");
    }

    public static string DexFilterHint(AppLanguage lang) =>
        T(lang, "탭하면 이 희귀도만 보기 · 다시 탭하면 전체",
            "Tap to show only this rarity · tap again to clear",
            "タップでこの希少度のみ表示・再タップで全体",
            "Toca para ver solo esta rareza · toca de nuevo para ver todo",
            "Touche pour n'afficher que cette rareté · touche à nouveau pour tout afficher",
            "Toque para ver só esta raridade · toque de novo para ver tudo",
            "Tippe, um nur diese Seltenheit zu sehen · tippe erneut für alle");

    public static string DexEmptyTitle(AppLanguage lang) =>
        T(lang, "아직 포켓몬을 잡지 못했어요!", "No Pokémon caught yet!",
            "まだポケモンを捕まえていません！", "¡Todavía no has capturado ningún Pokémon!",
            "Aucun Pokémon capturé pour l'instant !", "Você ainda não capturou nenhum Pokémon!",
            "Du hast noch kein Pokémon gefangen!");

    public static string DexEmptyHint(AppLanguage lang) =>
        T(lang, "토큰을 쓰면 첫 포켓몬이 부화해서 도감에 들어와요.",
            "Spend tokens to hatch your first Pokémon.",
            "トークンを使うと最初のポケモンが孵化して図鑑に加わります。",
            "Usa tokens para eclosionar tu primer Pokémon.",
            "Dépense des tokens pour faire éclore ton premier Pokémon.",
            "Use tokens para chocar seu primeiro Pokémon.",
            "Verwende Tokens, damit dein erstes Pokémon schlüpft.");

    public static string BagEmptyTitle(AppLanguage lang) =>
        T(lang, "아직 가방이 비어있어요!", "Your bag is empty!", "バッグはまだ空っぽです！",
            "¡Tu bolsa todavía está vacía!", "Ton sac est encore vide !",
            "Sua bolsa ainda está vazia!", "Dein Beutel ist noch leer!");

    public static string UseItemLabel(AppLanguage lang) =>
        T(lang, "사용하기", "Use", "つかう", "Usar", "Utiliser", "Usar", "Verwenden");

    public static string UseLabel(AppLanguage lang) =>
        T(lang, "사용", "Use", "つかう", "Usar", "Utiliser", "Usar", "Verwenden");

    public static string UseOnCurrent(AppLanguage lang, string name) =>
        T(lang, $"{name}에게 사용할까요?", $"Use on {name}?", $"{name} に使いますか？",
            $"¿Usar en {name}?", $"Utiliser sur {name} ?", $"Usar em {name}?",
            $"Bei {name} verwenden?");

    public static string UseAfterHatch(AppLanguage lang) =>
        T(lang, "부화 후 사용할 수 있어요", "Usable after hatching", "孵化後に使えます",
            "Se puede usar después de eclosionar", "Utilisable après l'éclosion",
            "Dá para usar depois que chocar", "Nach dem Schlüpfen verwendbar");

    public static string UseNeedsPokemon(AppLanguage lang) =>
        T(lang, "사용할 포켓몬이 없어요", "No Pokémon to use it on", "使えるポケモンがいません",
            "No hay ningún Pokémon en quien usarlo", "Aucun Pokémon sur qui l'utiliser",
            "Nenhum Pokémon para usar o item", "Kein Pokémon, bei dem du es verwenden kannst");

    public static string CandyGraduatesHint(AppLanguage lang) =>
        T(lang, "이 포켓몬은 졸업할 것으로 예상돼요.", "Expected to graduate.", "卒業する見込みです。",
            "Se espera que se gradúe.", "Devrait terminer sa croissance.", "Deve se formar.",
            "Schließt voraussichtlich sein Training ab.");

    public static string CandyCarryoverXP(AppLanguage lang, string xp) =>
        T(lang, $"예상 진화 후 이월 경험치: {xp} XP.", $"Expected after evolution: {xp} XP carried over.",
            $"進化後の予想繰越経験値：{xp} XP。", $"Tras evolucionar: {xp} XP de remanente previsto.",
            $"Après évolution : {xp} XP de report prévu.", $"Após evoluir: previsão de {xp} XP restantes.",
            $"Nach der Entwicklung: voraussichtlich {xp} EP übertragen.");

    public static string CandyDiscardedXP(AppLanguage lang, string xp) =>
        T(lang, $"졸업 시 남는 {xp} XP는 사라져요.", $"On graduation, {xp} leftover XP will be discarded.",
            $"卒業時、余った{xp} XPは失われます。", $"Al graduarse, se perderán {xp} XP sobrantes.",
            $"À la fin de la croissance, les {xp} XP restants seront perdus.",
            $"Ao se formar, {xp} XP restantes serão descartadas.",
            $"Beim Trainingsabschluss verfallen {xp} überschüssige EP.");

    public static string MintEffectHint(AppLanguage lang) =>
        T(lang, "성격 랜덤 변경", "Random nature", "せいかくランダム変更", "Naturaleza aleatoria",
            "Nature aléatoire", "Natureza aleatória", "Zufälliges Wesen");

    public static string ShinyCharmEffectHint(AppLanguage lang) =>
        T(lang, "이로치 확률 ↑ · 적용 중", "Shiny rate ↑ · active", "色違い率↑ · 適用中",
            "Prob. variocolor ↑ · activo", "Taux chromatique ↑ · actif", "Chance shiny ↑ · ativo",
            "Schillerchance ↑ · aktiv");

    // Feedback strings

    public static string SelectShopRowFirst(AppLanguage lang) =>
        T(lang, "상점 항목을 먼저 선택하세요", "Select a shop row first",
            "先にショップ項目を選んでください", "Selecciona primero una fila de la tienda",
            "Sélectionne d'abord une ligne de la boutique", "Selecione primeiro um item da loja",
            "Zuerst einen Laden-Eintrag auswählen");

    public static string BoughtItem(AppLanguage lang, string name) =>
        T(lang, $"구매함: {name}", $"Bought {name}", $"購入: {name}", $"Comprado: {name}",
            $"Acheté : {name}", $"Comprou {name}", $"Gekauft: {name}");

    public static string CannotBuyItem(AppLanguage lang, string name) =>
        T(lang, $"아직 구매할 수 없어요: {name}", $"Cannot buy {name} yet",
            $"まだ購入できません: {name}", $"Todavía no puedes comprar {name}",
            $"Impossible d'acheter {name} pour l'instant", $"Ainda não pode comprar {name}",
            $"{name} kann noch nicht gekauft werden");

    public static string CandyGraduated(AppLanguage lang) =>
        T(lang, "이상한 사탕 사용 — 도감에 졸업!", "Candy used — graduated into the dex!",
            "ふしぎなアメ使用 — 図鑑に卒業！", "Caramelo usado — ¡graduado a la Pokédex!",
            "Bonbon utilisé — diplômé dans le Pokédex !", "Doce usado — formou na Pokédex!",
            "Bonbon eingesetzt — in den Pokédex entlassen!");

    public static string CandyEvolved(AppLanguage lang) =>
        T(lang, "이상한 사탕 사용 — 진화!", "Candy used — evolved!", "ふしぎなアメ使用 — 進化！",
            "Caramelo usado — ¡evolucionó!", "Bonbon utilisé — a évolué !",
            "Doce usado — evoluiu!", "Bonbon eingesetzt — entwickelt!");

    public static string CandyProgressed(AppLanguage lang) =>
        T(lang, "이상한 사탕 사용 — +100M XP", "Candy used — +100M XP",
            "ふしぎなアメ使用 — +100M XP", "Caramelo usado — +100M XP",
            "Bonbon utilisé — +100M XP", "Doce usado — +100M XP", "Bonbon eingesetzt — +100M XP");

    public static string NoCandy(AppLanguage lang) =>
        T(lang, "사용할 이상한 사탕이 없어요", "No candy to use", "使うふしぎなアメがありません",
            "No hay caramelos que usar", "Aucun bonbon à utiliser", "Não há doce para usar",
            "Kein Bonbon vorhanden");

    public static string UsedCandies(AppLanguage lang, int count) =>
        T(lang, $"이상한 사탕 {count}개 사용", $"Used {count} candies", $"ふしぎなアメを{count}個使用",
            $"Usados {count} caramelos", $"{count} bonbons utilisés", $"Usou {count} doces",
            $"{count} Bonbons eingesetzt");

    public static string MintUsed(AppLanguage lang, string nature) =>
        T(lang, $"민트 사용 — 성격이 {nature}(으)로 변했어요", $"Mint used — nature is now {nature}",
            $"ミント使用 — 性格が{nature}になりました", $"Menta usada — naturaleza ahora {nature}",
            $"Menthe utilisée — nature désormais {nature}", $"Menta usada — natureza agora {nature}",
            $"Minze eingesetzt — Wesen ist jetzt {nature}");

    public static string NoMint(AppLanguage lang) =>
        T(lang, "사용할 민트가 없어요", "No mint to use", "使うミントがありません",
            "No hay menta que usar", "Aucune menthe à utiliser", "Não há menta para usar",
            "Keine Minze vorhanden");

    public static string NoCombatDetails(AppLanguage lang, int speciesID) =>
        T(lang, $"#{speciesID} 상세 정보가 없어요", $"No combat details for #{speciesID}",
            $"#{speciesID}の詳細情報がありません", $"Sin detalles de combate para #{speciesID}",
            $"Pas de détails de combat pour #{speciesID}",
            $"Sem detalhes de combate para #{speciesID}", $"Keine Kampfdetails für #{speciesID}");

    public static string OwnedSuffix(AppLanguage lang) =>
        T(lang, "(보유함)", "(owned)", "(所持)", "(en posesión)", "(possédé)", "(adquirido)",
            "(besessen)");

    public static string NeedMoreTokens(AppLanguage lang) =>
        T(lang, "(토큰 부족)", "(need more tokens)", "(トークン不足)", "(faltan tokens)",
            "(jetons insuffisants)", "(faltam tokens)", "(zu wenig Tokens)");

    // Detail window

    public static string PokemonDetailsTitle(AppLanguage lang) =>
        T(lang, "포켓몬 상세", "Pokémon details", "ポケモン詳細", "Detalles del Pokémon",
            "Détails du Pokémon", "Detalhes do Pokémon", "Pokémon-Details");

    public static string IndividualsTitle(AppLanguage lang) =>
        T(lang, "개체", "Individuals", "個体", "Ejemplares", "Individus", "Indivíduos",
            "Individuen");

    public static string KnownMovesTitle(AppLanguage lang) =>
        T(lang, "배운 기술", "Known moves", "覚えている技", "Movimientos conocidos",
            "Capacités connues", "Golpes conhecidos", "Erlernte Attacken");

    public static string BaseStatsTitle(AppLanguage lang) =>
        T(lang, "기본 능력치", "Base stats", "種族値", "Estadísticas base", "Stats de base",
            "Atributos base", "Basiswerte");

    public static string PossibleAbilitiesTitle(AppLanguage lang) =>
        T(lang, "가능한 특성", "Possible abilities", "可能な特性", "Habilidades posibles",
            "Talents possibles", "Habilidades possíveis", "Mögliche Fähigkeiten");

    public static string MoveListCount(AppLanguage lang, int count) =>
        T(lang, $"전체 기술 목록 {count}개", $"Complete move list · {count}", $"全技リスト・{count}",
            $"Lista completa · {count}", $"Liste complète · {count}", $"Lista completa · {count}",
            $"Vollständige Attackenliste · {count}");

    public static string HiddenMark(AppLanguage lang) =>
        T(lang, "숨김", "hidden", "隠れ", "oculta", "caché", "oculta", "versteckt");

    public static string AbilityLabel(AppLanguage lang) =>
        T(lang, "특성", "Ability", "特性", "Habilidad", "Talent", "Habilidade", "Fähigkeit");

    public static string HeightLabel(AppLanguage lang) =>
        T(lang, "키", "Height", "高さ", "Altura", "Taille", "Altura", "Größe");

    public static string WeightLabel(AppLanguage lang) =>
        T(lang, "몸무게", "Weight", "重さ", "Peso", "Poids", "Peso", "Gewicht");

    public static string BaseTotalLabel(AppLanguage lang) =>
        T(lang, "합계", "Base total", "合計", "Total base", "Total de base", "Total base",
            "Basiswertsumme");

    public static string GenderLabel(AppLanguage lang, PokemonGender? gender) => gender switch
    {
        PokemonGender.Male => T(lang, "수컷", "male", "オス", "macho", "mâle", "macho", "männlich"),
        PokemonGender.Female => T(lang, "암컷", "female", "メス", "hembra", "femelle", "fêmea",
            "weiblich"),
        PokemonGender.Genderless => T(lang, "무성", "genderless", "性別不明", "sin género",
            "asexué", "sem gênero", "geschlechtslos"),
        _ => ""
    };

    public static string StatLabel(AppLanguage lang, string stat) => stat switch
    {
        "hp" => "HP",
        "attack" => T(lang, "공격", "Atk", "こうげき", "Ataque", "Atq.", "Ataque", "Angr."),
        "defense" => T(lang, "방어", "Def", "ぼうぎょ", "Defensa", "Déf.", "Defesa", "Vert."),
        "special-attack" => T(lang, "특공", "Sp.Atk", "とくこう", "At.Esp.", "Atq.Sp.",
            "Atq.Esp.", "Sp.-Ang."),
        "special-defense" => T(lang, "특방", "Sp.Def", "とくぼう", "Def.Esp.", "Déf.Sp.",
            "Def.Esp.", "Sp.-Vert."),
        "speed" => T(lang, "스피드", "Spe", "すばやさ", "Velocidad", "Vitesse", "Velocidade",
            "Init."),
        _ => stat
    };

    public static string MoveMethodLabel(AppLanguage lang, string method, int level) =>
        method switch
        {
            "level-up" => level > 0 ? $"Lv. {level}"
                : T(lang, "시작", "Start", "基本", "Inicio", "Départ", "Inicial", "Start"),
            "machine" => "TM",
            "egg" => T(lang, "교배", "Egg", "タマゴ", "Huevo", "Œuf", "Ovo", "Ei"),
            "tutor" => T(lang, "가르침", "Tutor", "教え", "Tutor", "Maître", "Tutor", "Tutor"),
            _ => method.Replace('-', ' ')
        };

    // Update notifications (port of the macOS UpdateChecker strings)

    public static string UpdateAvailable(AppLanguage lang, string version, string current) =>
        T(lang, $"🆕 v{version} 사용 가능 (현재 {current})",
            $"🆕 v{version} available (you have {current})",
            $"🆕 v{version} が利用可能（現在 {current}）",
            $"🆕 v{version} disponible (tienes {current})",
            $"🆕 v{version} disponible (tu as {current})",
            $"🆕 v{version} disponível (você tem {current})",
            $"🆕 v{version} verfügbar (installiert: {current})");

    public static string UpdateButton(AppLanguage lang) =>
        T(lang, "업데이트", "Update", "更新", "Actualizar", "Mettre à jour", "Instalar",
            "Aktualisieren");

    public static string SkipThisVersion(AppLanguage lang) =>
        T(lang, "이 버전 건너뛰기", "Skip this version", "このバージョンをスキップ",
            "Omitir esta versión", "Ignorer cette version", "Ignorar esta versão",
            "Diese Version überspringen");

    public static string SkippedVersionText(AppLanguage lang, string version) =>
        T(lang, $"v{version}을 건너뛰었어요",
            $"You skipped v{version}",
            $"v{version} をスキップしました",
            $"Omitiste la v{version}",
            $"Tu as ignoré la v{version}",
            $"Você ignorou a v{version}",
            $"Du hast v{version} übersprungen");

    public static string ShowSkippedAgain(AppLanguage lang) =>
        T(lang, "다시 알리기", "Show again", "もう一度表示", "Mostrar de nuevo",
            "Afficher à nouveau", "Mostrar de novo", "Wieder anzeigen");

    public static string UpdateSectionTitle(AppLanguage lang) =>
        T(lang, "업데이트", "Updates", "アップデート", "Actualizaciones", "Mises à jour",
            "Atualizações", "Aktualisierungen");

    public static string UpdateNotificationsLabel(AppLanguage lang) =>
        T(lang, "업데이트 알림", "Update notifications", "アップデート通知",
            "Notificaciones de actualización", "Notifications de mise à jour",
            "Notificações de atualização", "Hinweise auf Aktualisierungen");

    public static string CheckForUpdatesLabel(AppLanguage lang) =>
        T(lang, "업데이트 확인", "Check for updates", "アップデートを確認",
            "Buscar actualizaciones", "Rechercher des mises à jour", "Buscar atualizações",
            "Nach Aktualisierungen suchen");

    public static string CheckNowButton(AppLanguage lang) =>
        T(lang, "지금 확인", "Check now", "今すぐ確認", "Comprobar ahora", "Vérifier maintenant",
            "Buscar agora", "Jetzt prüfen");

    public static string UpdateFound(AppLanguage lang, string version) =>
        T(lang, $"새 버전 v{version} 있어요", $"Version {version} is available",
            $"バージョン {version} が利用可能です", $"La versión {version} está disponible",
            $"La version {version} est disponible", $"A versão {version} está disponível",
            $"Version {version} ist verfügbar");

    public static string UpToDate(AppLanguage lang, string version) =>
        T(lang, $"최신 버전이에요 (v{version})", $"You're on the latest (v{version})",
            $"最新です (v{version})", $"Tienes la última versión (v{version})",
            $"Tu as la dernière version (v{version})", $"Você está na última versão (v{version})",
            $"Du hast die neueste Version (v{version})");

    // Representative Pokémon

    public static string RepresentativePokemonLabel(AppLanguage lang) =>
        T(lang, "대표 포켓몬", "Representative Pokémon", "代表ポケモン", "Pokémon representativo",
            "Pokémon représentatif", "Pokémon representativo", "Repräsentatives Pokémon");

    public static string RepresentativeFollowCurrent(AppLanguage lang) =>
        T(lang, "현재 포켓몬 따라가기", "Follow current companion", "現在のポケモンに合わせる",
            "Seguir al compañero actual", "Suivre le compagnon actuel", "Seguir o companheiro atual",
            "Aktuellem Begleiter folgen");

    public static string RepresentativeChooseFromDex(AppLanguage lang) =>
        T(lang, "도감에서 선택…", "Choose in Pokédex…", "図鑑で選ぶ…", "Elegir en la Pokédex…",
            "Choisir dans le Pokédex…", "Escolher na Pokédex…", "Im Pokédex auswählen…");

    public static string RepresentativeSet(AppLanguage lang) =>
        T(lang, "대표로 설정", "Set as representative", "代表ポケモンに設定",
            "Establecer como representante", "Définir comme représentatif",
            "Definir como representante", "Als repräsentativ festlegen");

    public static string RepresentativeBadge(AppLanguage lang) =>
        T(lang, "대표", "Representative", "代表", "Representante", "Représentatif", "Representante",
            "Repräsentativ");

    // Usage home

    public static string TodayTokensHeader(AppLanguage lang) =>
        T(lang, "오늘 사용한 토큰", "Today's tokens", "本日のトークン", "Tokens de hoy",
            "Tokens du jour", "Tokens de hoje", "Heute verbrauchte Tokens");

    public static string ThisWeekLabel(AppLanguage lang) =>
        T(lang, "이번 주", "This week", "今週", "Esta semana", "Cette semaine", "Esta semana",
            "Diese Woche");

    public static string ThisMonthLabel(AppLanguage lang) =>
        T(lang, "이번 달", "This month", "今月", "Este mes", "Ce mois-ci", "Este mês",
            "Dieser Monat");

    public static string TokenInputLabel(AppLanguage lang) =>
        T(lang, "입력", "Input", "入力", "Entrada", "Entrée", "Entrada", "Eingabe");

    public static string TokenOutputLabel(AppLanguage lang) =>
        T(lang, "출력", "Output", "出力", "Salida", "Sortie", "Saída", "Ausgabe");

    public static string TokenCacheWriteLabel(AppLanguage lang) =>
        T(lang, "캐시 쓰기", "Cache write", "キャッシュ書込", "Escritura caché", "Écriture cache",
            "Gravação cache", "Cache schreiben");

    public static string TokenCacheReadLabel(AppLanguage lang) =>
        T(lang, "캐시 읽기", "Cache read", "キャッシュ読込", "Lectura caché", "Lecture cache",
            "Leitura cache", "Cache lesen");

    // Engine event / notice texts

    public static string EventHatch(AppLanguage lang, string name, bool shiny) => shiny
        ? T(lang, $"이로치 {name}이(가) 알에서 태어났어요!", $"A shiny {name} hatched from the egg!",
            $"色違いの {name} がタマゴから生まれました！", $"¡Nació un {name} variocolor del huevo!",
            $"Un {name} chromatique est sorti de l'œuf !", $"Nasceu um {name} shiny do ovo!",
            $"Ein schillerndes {name} ist aus dem Ei geschlüpft!")
        : T(lang, $"알에서 {name}이(가) 나왔어요!", $"{name} hatched from the egg!",
            $"タマゴから {name} が生まれました！", $"¡{name} salió del huevo!",
            $"{name} est sorti de l'œuf !", $"{name} saiu do ovo!",
            $"{name} ist aus dem Ei geschlüpft!");

    public static string EventEvolve(AppLanguage lang, string name) =>
        T(lang, $"{name}(으)로 진화했어요!", $"Evolved into {name}!", $"{name} に進化しました！",
            $"¡Evolucionó a {name}!", $"A évolué en {name} !", $"Evoluiu para {name}!",
            $"Hat sich zu {name} entwickelt!");

    public static string EventGraduate(AppLanguage lang, string name) =>
        T(lang, $"{name} 졸업 → 도감에 보존. 새 Token Egg가 도착했어요!",
            $"{name} graduated → saved to the dex. A new Token Egg has arrived!",
            $"{name} 卒業 → 図鑑に保存。新しいToken Eggが届きました！",
            $"{name} se graduó → guardado en la Pokédex. ¡Ha llegado un nuevo Token Egg!",
            $"{name} a été diplômé → conservé dans le Pokédex. Un nouveau Token Egg est arrivé !",
            $"{name} se formou → guardado na Pokédex. Chegou um novo Token Egg!",
            $"{name} verabschiedet sich → im Pokédex gespeichert. Ein neues Token Egg ist da!");

    public static string EventDittoReveal(AppLanguage lang, string disguise, bool shiny) => shiny
        ? T(lang, $"{disguise}인 줄 알았는데 — 이로치 메타몽이었어요! (1/64)",
            $"You thought it was {disguise} — it was a shiny Ditto! (1 in 64)",
            $"{disguise} だと思ってた… 色違いのメタモンでした！(1/64)",
            $"Pensabas que era {disguise} — ¡era un Ditto variocolor! (1 entre 64)",
            $"Tu croyais que c'était {disguise} — c'était un Métamorph chromatique ! (1 sur 64)",
            $"Você achava que era {disguise} — era um Ditto shiny! (1 em 64)",
            $"Du dachtest, es wäre {disguise} – dabei war es ein schillerndes Ditto! (1/64)")
        : T(lang, $"{disguise}인 줄 알았는데 — 사실은 메타몽이었어요!",
            $"You thought it was {disguise} — it was Ditto all along!",
            $"{disguise} だと思ってた… 実はメタモンでした！",
            $"Pensabas que era {disguise} — ¡en realidad era Ditto!",
            $"Tu croyais que c'était {disguise} — c'était Métamorph depuis le début !",
            $"Você achava que era {disguise} — era um Ditto o tempo todo!",
            $"Du dachtest, es wäre {disguise} – dabei war es die ganze Zeit Ditto!");

    public static string EventRelease(AppLanguage lang, string name) =>
        T(lang, $"{name}을(가) 놓아줬어요 — 새 알이 도착했어요.",
            $"{name} released — a new egg has arrived.",
            $"{name} を手放しました — 新しいタマゴが届きました。",
            $"{name} liberado — ha llegado un nuevo huevo.",
            $"{name} relâché — un nouvel œuf est arrivé.",
            $"{name} solto — um novo ovo chegou.",
            $"{name} verabschiedet — ein neues Ei ist da.");
}
