# ディレクトリ構成

実装言語は C#(.NET 10)。`src/` 以下を Pass ごと(Frontend / Optimizer / Backend)に分割する。
設計は Clang / LLVM を参考にする。

```
chibikko-cc/
├── Chibikko.slnx
├── src/                            # プロジェクト Chibikko(Exe)
│   ├── Chibikko.csproj
│   ├── Program.cs                  # CLI エントリ
│   ├── Compiler/                   # 名前空間は Chibikko(Compiler 型との衝突を避ける)
│   │   ├── Compiler.cs             # 3 つの Pipeline を順に呼ぶ
│   │   ├── IFrontendPipeline.cs    # ソース → IR
│   │   ├── IOptimizationPipeline.cs # IR → IR
│   │   └── IBackendPipeline.cs     # IR → アセンブリ
│   ├── Common/
│   │   ├── CompileException.cs     # エラー報告(行・列は Offset から計算)
│   │   ├── Type.cs                 # 型の定義、型比較、通常算術変換
│   │   └── Ir/
│   │       ├── Ir.cs               # 命令・基本ブロック・関数の定義
│   │       └── Dump.cs             # IR ダンプ(-emit-ir 用)
│   ├── Frontend/
│   │   ├── Token.cs                # TokenKind と Token(Kind, Offset, Length)
│   │   ├── Lexer.cs                # ソース → Token(逐次)
│   │   ├── TokenStream.cs          # Lexer の先読みバッファ(Peek / Consume / Expect)
│   │   ├── Ast.cs                  # AST ノード
│   │   ├── Parser.cs               # Token → AST(grammar.md と 1:1)
│   │   ├── SemanticAnalyzer.cs     # 型付け、スコープ解決、シグネチャ検査
│   │   ├── IrGenerator.cs          # 型付き AST → IR
│   │   └── FrontendPipeline.cs     # Lexer から IrGenerator までの接続
│   ├── Optimizer/
│   │   ├── OptimizationPipeline.cs # Pass の登録と実行順の管理
│   │   ├── ConstantFolding.cs      # 定数畳み込み
│   │   └── DeadCodeElimination.cs  # 不要コード削除
│   └── Backend/
│       ├── BackendPipeline.cs      # CodeGenerator と RegisterAllocation の接続
│       ├── CodeGenerator.cs        # IR → x86-64 アセンブリ
│       └── RegisterAllocation.cs   # レジスタ割り当て
└── tests/
    └── Chibikko.Tests/             # xUnit
        └── Frontend/               # src/ と同じ階層で分ける(Optimizer/ Backend/ なども同様)
            └── TokenTests.cs
```

名前空間はディレクトリに対応させる(`Chibikko.Frontend` `Chibikko.Common.Ir` など)。
テストは `src/` のディレクトリ構成をなぞって配置し、ファイル名は `<対象の型>Tests.cs`、名前空間は `Chibikko.Tests.<ディレクトリ>` とする。

## 処理の流れ

```
ソース → [Frontend] → IR → [Optimizer] → IR → [Backend] → アセンブリ
```

| 段階 | 入力 | 出力 |
|------|------|------|
| Lexer | ソース文字列 | Token(逐次) |
| Parser | Token(逐次) | AST(型なし) |
| SemanticAnalyzer | AST | 型付き AST |
| IrGenerator | 型付き AST | IR |
| Optimizer | IR | IR |
| CodeGenerator | IR | x86-64 アセンブリ |

## 依存関係

```
Program → Compiler(Interface)
   │
   └→ Frontend / Optimizer / Backend → Compiler(Interface), Common.Ir
```

- Frontend / Optimizer / Backend は互いに直接依存しない。3 者とも `Compiler` の Interface と `Common` のみを参照する。`Compiler` は Interface のみに依存し、実装の組み立ては `Program` が行う。
- Optimizer は空でも動作する(IR をそのまま通す)。
- Backend は IR のみを入力とし、AST を参照しない。

## 各層の責務

- Token は種別とソース上の位置(Offset, Length)だけを持つ。リテラルの値変換とサフィックス判定は後段で行う(Clang の方式)。
- Parser は `Lexer.Lex()` で Token を 1 つずつ取得する。Token 列を事前に作らない(Clang の方式)。先読みのバッファは `TokenStream` が持ち、Parser はそれを介して Token を読む。Token 列をまとめて得る `Tokenize()` はテスト側の拡張メソッド(`tests/Chibikko.Tests/Frontend/LexerExtensions.cs`)とする。
- Parser は構文木を作るだけで、型の判断をしない。型の判断は SemanticAnalyzer が行う。
- 暗黙の型変換は SemanticAnalyzer が `ImplicitCastExpression` として AST に挿入し(Clang の `ImplicitCastExpr`)、IrGenerator が `convert` 命令に変換する。Backend は暗黙変換を扱わない。
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
