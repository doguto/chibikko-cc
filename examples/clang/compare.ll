; ModuleID = 'examples/compare.c'
source_filename = "examples/compare.c"
target datalayout = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-i128:128-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

; Function Attrs: noinline nounwind uwtable
define dso_local i32 @main() #0 {
entry:
  %retval = alloca i32, align 4
  %a = alloca i32, align 4
  %b = alloca i32, align 4
  %r = alloca i32, align 4
  store i32 0, ptr %retval, align 4
  store i32 5, ptr %a, align 4
  store i32 7, ptr %b, align 4
  store i32 0, ptr %r, align 4
  %0 = load i32, ptr %a, align 4
  %1 = load i32, ptr %b, align 4
  %cmp = icmp slt i32 %0, %1
  br i1 %cmp, label %if.then, label %if.end

if.then:                                          ; preds = %entry
  %2 = load i32, ptr %r, align 4
  %add = add nsw i32 %2, 1
  store i32 %add, ptr %r, align 4
  br label %if.end

if.end:                                           ; preds = %if.then, %entry
  %3 = load i32, ptr %a, align 4
  %4 = load i32, ptr %b, align 4
  %cmp1 = icmp sge i32 %3, %4
  br i1 %cmp1, label %if.then2, label %if.else

if.then2:                                         ; preds = %if.end
  %5 = load i32, ptr %r, align 4
  %add3 = add nsw i32 %5, 10
  store i32 %add3, ptr %r, align 4
  br label %if.end5

if.else:                                          ; preds = %if.end
  %6 = load i32, ptr %r, align 4
  %add4 = add nsw i32 %6, 100
  store i32 %add4, ptr %r, align 4
  br label %if.end5

if.end5:                                          ; preds = %if.else, %if.then2
  %7 = load i32, ptr %a, align 4
  %8 = load i32, ptr %b, align 4
  %cmp6 = icmp ne i32 %7, %8
  br i1 %cmp6, label %if.then7, label %if.end9

if.then7:                                         ; preds = %if.end5
  %9 = load i32, ptr %r, align 4
  %add8 = add nsw i32 %9, 1000
  store i32 %add8, ptr %r, align 4
  br label %if.end9

if.end9:                                          ; preds = %if.then7, %if.end5
  %10 = load i32, ptr %r, align 4
  ret i32 %10
}

attributes #0 = { noinline nounwind uwtable "frame-pointer"="all" "min-legal-vector-width"="0" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cmov,+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }

!llvm.module.flags = !{!0, !1, !2, !3, !4}
!llvm.ident = !{!5}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 8, !"PIC Level", i32 2}
!2 = !{i32 7, !"PIE Level", i32 2}
!3 = !{i32 7, !"uwtable", i32 2}
!4 = !{i32 7, !"frame-pointer", i32 2}
!5 = !{!"Ubuntu clang version 18.1.3 (1ubuntu1)"}
