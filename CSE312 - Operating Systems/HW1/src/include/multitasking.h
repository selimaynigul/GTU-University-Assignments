 
#ifndef __MYOS__MULTITASKING_H
#define __MYOS__MULTITASKING_H

#include <common/types.h>
#include <gdt.h>

namespace myos
{
    namespace hardwarecommunication {
        class InterruptHandler;
    }
    
    struct CPUState {
        common::uint32_t eax;
        common::uint32_t ebx;
        common::uint32_t ecx;
        common::uint32_t edx;
        common::uint32_t esi;
        common::uint32_t edi;
        common::uint32_t ebp;
        common::uint32_t error;
        common::uint32_t eip;
        common::uint32_t cs;
        common::uint32_t eflags;
        common::uint32_t esp;
        common::uint32_t ss;        
    } __attribute__((packed));
    
    typedef enum {
        READY,
        RUNNING,
        BLOCKED,
        TERMINATED,
    } ProcessState;

    class Task {

        friend class TaskManager;
        private:
            common::uint8_t stack[4096]; // 4 KiB
            CPUState* cpustate;
            common::uint32_t pid;
            common::uint32_t ppid;
            common::uint32_t waitpid;
            common::uint32_t waitnum;
            ProcessState state;
            bool waitparent; 
            int priority;
        public:
            Task();
            Task(GlobalDescriptorTable *gdt, void entrypoint());
            ~Task();
    };
    
    class TaskManager {

        friend class hardwarecommunication::InterruptHandler;
        private:
            Task tasks[256];
            int numTasks;
            int currentTask;
            GlobalDescriptorTable* gdt;

        protected: 
            common::uint32_t ExecTask(void entrypoint());
            common::uint32_t ForkTask(CPUState* cpustate);
            common::uint32_t ForkTaskThirdStrategy(CPUState* cpustate);
            common::uint32_t ForkTaskDynamic(CPUState* cpustate);
            bool WaitTask(common::uint32_t pid);
            bool ExitTask();
            common::uint32_t getpid();

        public:
            TaskManager(GlobalDescriptorTable* gdt); 
            ~TaskManager();
            bool InitTask(Task*task);
            void AddTask(Task *src, Task *dest);
            CPUState* ScheduleRobinRound(CPUState* cpustate, int interruptCount);
            CPUState* SchedulePreemptive(CPUState* cpustate, int interruptCount);
            CPUState* ScheduleDynamic(CPUState* cpustate, int interruptCount);
            void PrintProcessTable();
    }; 
}

#endif