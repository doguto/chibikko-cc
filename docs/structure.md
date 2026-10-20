# ディレクトリ構成

実装言語は C#(.NET 10)。`src/` 以下を Pass ごと(Frontend / Optimizer / Backend)に分割する。
設計は Clang / LLVM を参考にする。

```
chibikko-cc/
├── Chibikko.slnx
├── src/                         # プロジェクト Chibikko(Exe)
│   ├── Chibikko.csproj
│   ├── Program.cs               # CLI エントリ
│   ├── Common/
│   │   ├── Util.cs              # エラー報告(行・列は Offset から計算)
│   │   ├── Type.cs              # 型の定義、型比較、通常算術変換
│   │   └── Ir/
│   │       ├── Ir.cs            # 命令・基本ブロック・関数の定義
│   │       └── Dump.cs          # IR ダンプ(-emit-ir 用)
│   ├── Frontend/
│   │   ├── Token.cs             # TokenKind と Token(Kind, Offset, Length)
│   │   ├── Lexer.cs             # ソース → Token 列
│   │   ├── Ast.cs               # AST ノード
│   │   ├── Parser.cs            # Token 列 → AST(grammar.md と 1:1)
│   │   ├── Sema.cs              # 型付け、スコープ解決、シグネチャ検査
│   │   └── IrGen.cs             # 型付き AST → IR
│   ├── Optimizer/
│   │   ├── Opt.cs               # Pass の登録と実行順の管理
│   │   ├── ConstFold.cs         # 定数畳み込み
│   │   └── Dce.cs               # 不要コード削除
│   └── Backend/
│       ├── CodeGen.cs           # IR → x86-64 アセンブリ
│       └── RegAlloc.cs          # レジスタ割り当て
└── tests/
    └── Chibikko.Tests/          # xUnit
```

名前空間はディレクトリに対応させる(`Chibikko.Frontend` `Chibikko.Common.Ir` など)。

## 処理の流れ

```
ソース → [Frontend] → IR → [Optimizer] → IR → [Backend] → アセンブリ
```

| 段階 | 入力 | 出力 |
|------|------|------|
| Lexer | ソース文字列 | Token 列 |
| Parser | Token 列 | AST(型なし) |
| Sema | AST | 型付き AST |
| IrGen | 型付き AST | IR |
| Optimizer | IR | IR |
| CodeGen | IR | x86-64 アセンブリ |

## 依存関係

```
Program → Frontend → Common.Ir ← Optimizer
                         ↑
                      Backend
```

- Frontend / Optimizer / Backend は互いに直接依存しない。3 者とも `Common` のみを参照する。
- Optimizer は空でも動作する(IR をそのまま通す)。
- Backend は IR のみを入力とし、AST を参照しない。

## 各層の責務

- Token は種別とソース上の位置(Offset, Length)だけを持つ。リテラルの値変換とサフィックス判定は後段で行う(Clang の方式)。
- Parser は構文木を作るだけで、型の判断をしない。型の判断は Sema が行う。
- 暗黙の型変換は IrGen で明示的な IR 命令に変換する。Backend は暗黙変換を扱わない。
- IR は関数ごとの基本ブロックと 3 番地コードとする。値は型付き(`i1` `i32` `i64` `f32` `f64`)とする。
- 最初はローカル変数をスタックスロットとして扱う。SSA 化は必要になった時点で Pass として追加する。

## 開発コマンド

```
dotnet build
dotnet test
dotnet run --project src -- <file>
```

アセンブリ → 実行ファイルの変換には `gcc`(または `as` + `ld`)を使う。

## 拡張方針

ファイルが肥大化した場合に、サブディレクトリへ分割する。事前には分割しない。
