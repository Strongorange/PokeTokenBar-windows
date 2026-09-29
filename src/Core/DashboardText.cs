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
        T(lang, "이로치", "shiny", "色違い", "variocolor", "chromatique", "shiny", "schillernd");

    public static string RarityLabel(AppLanguage lang, Rarity rarity) => rarity switch
    {
        Rarity.Common => T(lang, "일반", "common", "ノーマル", "común", "commun", "comum",
            "gewöhnlich"),
        Rarity.Uncommon => T(lang, "고급", "uncommon", "アンコモン", "poco común", "peu commun",
            "incomum", "ungewöhnlich"),
        Rarity.Rare => T(lang, "희귀", "rare", "レア", "raro", "rare", "raro", "selten"),
        Rarity.Legendary => T(lang, "전설", "legendary", "伝説", "legendario", "légendaire",
            "lendário", "legendär"),
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
