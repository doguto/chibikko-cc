; ModuleID = 'examples/else_if.c'
source_filename = "examples/else_if.c"
target datalayout = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-i128:128-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

; Function Attrs: noinline nounwind uwtable
define dso_local i32 @classify(i32 noundef %n) #0 {
entry:
  %retval = alloca i32, align 4
  %n.addr = alloca i32, align 4
  store i32 %n, ptr %n.addr, align 4
  %0 = load i32, ptr %n.addr, align 4
  %cmp = icmp slt i32 %0, 0
  br i1 %cmp, label %if.then, label %if.else

if.then:                                          ; preds = %entry
  store i32 1, ptr %retval, align 4
  br label %return

if.else:                                          ; preds = %entry
  %1 = load i32, ptr %n.addr, align 4
  %cmp1 = icmp eq i32 %1, 0
  br i1 %cmp1, label %if.then2, label %if.else3

if.then2:                                         ; preds = %if.else
  store i32 2, ptr %retval, align 4
  br label %return

if.else3:                                         ; preds = %if.else
  %2 = load i32, ptr %n.addr, align 4
  %cmp4 = icmp slt i32 %2, 10
  br i1 %cmp4, label %if.then5, label %if.else6

if.then5:                                         ; preds = %if.else3
  store i32 3, ptr %retval, align 4
  br label %return

if.else6:                                         ; preds = %if.else3
  store i32 4, ptr %retval, align 4
  br label %return

return:                                           ; preds = %if.else6, %if.then5, %if.then2, %if.then
  %3 = load i32, ptr %retval, align 4
  ret i32 %3
}

; Function Attrs: noinline nounwind uwtable
define dso_local i32 @main() #0 {
entry:
  %retval = alloca i32, align 4
  store i32 0, ptr %retval, align 4
  %call = call i32 @classify(i32 noundef -5)
  %mul = mul nsw i32 %call, 1000
  %call1 = call i32 @classify(i32 noundef 0)
  %mul2 = mul nsw i32 %call1, 100
  %add = add nsw i32 %mul, %mul2
  %call3 = call i32 @classify(i32 noundef 7)
  %mul4 = mul nsw i32 %call3, 10
  %add5 = add nsw i32 %add, %mul4
  %call6 = call i32 @classify(i32 noundef 42)
  %add7 = add nsw i32 %add5, %call6
  ret i32 %add7
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
