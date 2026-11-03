; ModuleID = 'examples/literals.c'
source_filename = "examples/literals.c"
target datalayout = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-i128:128-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

; Function Attrs: noinline nounwind uwtable
define dso_local i64 @big() #0 {
entry:
  ret i64 100000
}

; Function Attrs: noinline nounwind uwtable
define dso_local float @half(float noundef %x) #0 {
entry:
  %x.addr = alloca float, align 4
  store float %x, ptr %x.addr, align 4
  %0 = load float, ptr %x.addr, align 4
  %div = fdiv float %0, 2.000000e+00
  ret float %div
}

; Function Attrs: noinline nounwind uwtable
define dso_local signext i8 @next(i8 noundef signext %c) #0 {
entry:
  %c.addr = alloca i8, align 1
  store i8 %c, ptr %c.addr, align 1
  %0 = load i8, ptr %c.addr, align 1
  %conv = sext i8 %0 to i32
  %add = add nsw i32 %conv, 1
  %conv1 = trunc i32 %add to i8
  ret i8 %conv1
}

; Function Attrs: noinline nounwind uwtable
define dso_local zeroext i1 @is_a(i8 noundef signext %c) #0 {
entry:
  %c.addr = alloca i8, align 1
  store i8 %c, ptr %c.addr, align 1
  %0 = load i8, ptr %c.addr, align 1
  %conv = sext i8 %0 to i32
  %cmp = icmp eq i32 %conv, 97
  ret i1 %cmp
}

; Function Attrs: noinline nounwind uwtable
define dso_local i32 @main() #0 {
entry:
  %retval = alloca i32, align 4
  %d = alloca double, align 8
  %f = alloca float, align 4
  %t = alloca i8, align 1
  %n = alloca i8, align 1
  %nl = alloca i8, align 1
  %l = alloca i64, align 8
  store i32 0, ptr %retval, align 4
  store double 3.140000e+00, ptr %d, align 8
  %call = call float @half(float noundef 5.000000e+00)
  store float %call, ptr %f, align 4
  store i8 1, ptr %t, align 1
  store i8 0, ptr %n, align 1
  store i8 10, ptr %nl, align 1
  %call1 = call i64 @big()
  store i64 %call1, ptr %l, align 8
  %0 = load i8, ptr %t, align 1
  %tobool = trunc i8 %0 to i1
  br i1 %tobool, label %if.then, label %if.end5

if.then:                                          ; preds = %entry
  %call2 = call signext i8 @next(i8 noundef signext 96)
  %call3 = call zeroext i1 @is_a(i8 noundef signext %call2)
  br i1 %call3, label %if.then4, label %if.end

if.then4:                                         ; preds = %if.then
  store i32 1, ptr %retval, align 4
  br label %return

if.end:                                           ; preds = %if.then
  br label %if.end5

if.end5:                                          ; preds = %if.end, %entry
  store i32 0, ptr %retval, align 4
  br label %return

return:                                           ; preds = %if.end5, %if.then4
  %1 = load i32, ptr %retval, align 4
  ret i32 %1
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
