#include <common/types.h>
#include <gdt.h>
#include <memorymanagement.h>
#include <hardwarecommunication/interrupts.h>
#include <syscalls.h>
#include <hardwarecommunication/pci.h>
#include <drivers/driver.h>
#include <drivers/keyboard.h>
#include <drivers/mouse.h>
#include <drivers/vga.h>
#include <drivers/ata.h>
#include <gui/desktop.h>
#include <gui/window.h>
#include <multitasking.h>
#include <drivers/amd_am79c973.h>

using namespace myos;
using namespace myos::common;
using namespace myos::drivers;
using namespace myos::hardwarecommunication;
using namespace myos::gui;

GlobalDescriptorTable gdt;
TaskManager taskManager(&gdt);
InterruptManager interrupts(0x20, &gdt, &taskManager);
SyscallHandler syscalls(&interrupts, 0x80);

void printf(char* str)
{
    static uint16_t* VideoMemory = (uint16_t*)0xb8000;

    static uint8_t x=0,y=0;

    for(int i = 0; str[i] != '\0'; ++i)
    {
        switch(str[i])
        {
            case '\n':
                x = 0;
                y++;
                break;
            default:
                VideoMemory[80*y+x] = (VideoMemory[80*y+x] & 0xFF00) | str[i];
                x++;
                break;
        }

        if(x >= 80)
        {
            x = 0;
            y++;
        }

        if(y >= 25)
        {
            for(y = 0; y < 25; y++)
                for(x = 0; x < 80; x++)
                    VideoMemory[80*y+x] = (VideoMemory[80*y+x] & 0xFF00) | ' ';
            x = 0;
            y = 0;
        }
    }
}

void printfHex(uint8_t key)
{
    char* foo = "00";
    char* hex = "0123456789ABCDEF";
    foo[0] = hex[(key >> 4) & 0xF];
    foo[1] = hex[key & 0xF];
    printf(foo);
}
void printfHex16(uint16_t key)
{
    printfHex((key >> 8) & 0xFF);
    printfHex( key & 0xFF);
}
void printfHex32(uint32_t key)
{
    printfHex((key >> 24) & 0xFF);
    printfHex((key >> 16) & 0xFF);
    printfHex((key >> 8) & 0xFF);
    printfHex( key & 0xFF);
}


void printfInt(int num)
{
    char buffer[32]; 
    int i = 0;
    if (num < 0) {
        printf("-");
        num = -num;
    }
    if (num == 0) {
        printf("0");
        return;
    }
    while (num != 0) {
        buffer[i++] = '0' + (num % 10);
        num /= 10;
    }
    buffer[i] = '\0';
    char reversed_buffer[32];
    int j = 0;

    for (int k = i - 1; k >= 0; --k) {
        reversed_buffer[j++] = buffer[k];
    }
    reversed_buffer[j] = '\0';
    printf(reversed_buffer); 
}

class PrintfKeyboardEventHandler : public KeyboardEventHandler
{
public:
    void OnKeyDown(char c)
    {
        char* foo = " ";
        foo[0] = c;
        printf(foo);
    }
};

class MouseToConsole : public MouseEventHandler
{
    int8_t x, y;
public:
    
    MouseToConsole()
    {
        uint16_t* VideoMemory = (uint16_t*)0xb8000;
        x = 40;
        y = 12;
        VideoMemory[80*y+x] = (VideoMemory[80*y+x] & 0x0F00) << 4
                            | (VideoMemory[80*y+x] & 0xF000) >> 4
                            | (VideoMemory[80*y+x] & 0x00FF);        
    }
    
    virtual void OnMouseMove(int xoffset, int yoffset)
    {
        static uint16_t* VideoMemory = (uint16_t*)0xb8000;
        VideoMemory[80*y+x] = (VideoMemory[80*y+x] & 0x0F00) << 4
                            | (VideoMemory[80*y+x] & 0xF000) >> 4
                            | (VideoMemory[80*y+x] & 0x00FF);

        x += xoffset;
        if(x >= 80) x = 79;
        if(x < 0) x = 0;
        y += yoffset;
        if(y >= 25) y = 24;
        if(y < 0) y = 0;

        VideoMemory[80*y+x] = (VideoMemory[80*y+x] & 0x0F00) << 4
                            | (VideoMemory[80*y+x] & 0xF000) >> 4
                            | (VideoMemory[80*y+x] & 0x00FF);
    }
    
};

void longTask() {
    printf("Long task started...\n");
    int n = 60000;
    int result = 0;
    for (int i = 0; i < n; ++i) {
        for (int j = 0; j < n; ++j) {
            result = i * j;
        }
    }
    printf("Long task is finished.\n");
}

void linearSearch() {
    printf("Linear search started...\n");
    const int n = 100;
    int arr[n];
    int size = n;
    int target = n - 2;

    for (int i = 0; i < n; i++) {
        arr[i] = i;
    }

    int result = 0;
    int val = -1;
    for (int i = 0; i < 9999000; i++) {
        for (int i = 0; i < size; ++i) {
            if (arr[i] == target) {
                result = i;
                val = i;
                break;
            }
        }
        result = -1;
    
    }
    if (val != -1) {
        result = val;
    }
    printf("Linear search finished... Target: ");
    printfInt(target);
    printf(", Target index: ");
    printfInt(result);
    printf("\n");
}

void binarySearch() {
    printf("Binary search started...\n");
    const int n = 100;
    int arr[100];
    int size = n;
    int target = 3;

    for (int i = 0; i < n; i++) {
        arr[i] = i;
    }
    int result = 0;
    int val = -1;
    for (int i = 0; i < 9999990; i++) {
        int left = 0;
        int right = size - 1;
        while (left <= right) {
            int mid = left + (right - left) / 2;
            
            if (arr[mid] == target) {
                result = mid; 
                val = mid;
                break;
            }
            if (arr[mid] < target) {
                left = mid + 1;
            } else {
                right = mid - 1; 
            }
        }
        result = -1;        
    }
    if (val != -1) {
        result = val;
    }
    printf("Binary search finished... Target: ");
    printfInt(target);
    printf(", Target index: ");
    printfInt(result);
    printf("\n");
}

void collatzSequence() {
    printf("Collatz started...\n");
    int num = 2;
    for (int i = 0; i < 999900000; i++) {
        while (num < 100000) {
            int n = num;
            while (n != 1) {
                if (n % 2 == 0) {
                    n = n / 2;
                } else {
                    n = 3 * n + 1;
                }
            }
            num++;
        }
    }
    printf("Collatz is finished.\n");
}


// parent prcoess creates three child process using nested forks
void partA_process() {   
    printf("PART A is running\n"); 

    int pid, pid2, pid3; // child pids

    int returnedPid = fork(&pid); // first fork

    if (returnedPid == pid) {
        returnedPid = fork(&pid2); // second fork
        if (returnedPid == pid2) {
            returnedPid = fork(&pid3); // third fork
            if (returnedPid == pid3) {   // PARENT PROCESS
                printf("All child processes are forked successfully.\n");
                waitpid(pid);
                waitpid(pid2);
                waitpid(pid3);
                printf("\n\n\n\n\n\nProcess table after all childs end:\n");
                taskManager.PrintProcessTable();
                printf("\nPART A is successfully completed.\n\n");
                exit();
            } 
            else { // FIRST CHILD
                collatzSequence();
                longTask();
                exit();
            }
        } 
        else { // SECOND CHILD
            collatzSequence();
            longTask();
            exit();
        } 
    } 
    else { // THIRD CHILD
        collatzSequence();
        longTask();
        exit();
    }
}

void partB_first() {
    printf("PART B the first strategy is running.\n");

    // Array of function pointers to the available programs
    void (*programs[])() = {longTask, collatzSequence, binarySearch, linearSearch};
    int numPrograms = sizeof(programs) / sizeof(programs[0]);

    int chosenProgramIndex = 0;
    void (*chosenProgram)() = programs[chosenProgramIndex];

    int childPids[10];
    int pid;

    for (int i = 0; i < 10; ++i) {
        int returnedPid = fork(&pid);
        if (returnedPid == pid) { // PARENT
            // Store the PID of the child process 
            childPids[i] = pid; 
        } 
        else { // CHILD
            chosenProgram();
            exit(); 
        } 
    }
    printf("10 child processes are started running...\n");

    // wait all child processes to finish 
    for (int i = 0; i < 10; ++i) {
        waitpid(childPids[i]);
    }

    printf("Process table after all childs end:\n");
    taskManager.PrintProcessTable();
    printf("PART B the first strategy is successfully finished.\n");

    exit();
}

void partB_second() {
    printf("PART B the second strategy is running.\n");
 
    void (*programs[])() = {longTask, collatzSequence, binarySearch, linearSearch};
    int numPrograms = sizeof(programs) / sizeof(programs[0]);
    int pids[6];

    // Choose 2 programs randomly
    int chosen_programs[2];
    int chosenProgramIndex = 0;
    for (int i = 0; i < 2; ++i) {
        chosen_programs[i] = chosenProgramIndex++; // Store the index of the chosen program
    }

    // Load each chosen program 3 times
    int index = 0;
    for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 2; ++j) {
            int pid;
            int returnedPid = fork(&pid);
            if (returnedPid == pid) {
                pids[index++] = pid;
            }
            else {
                programs[chosen_programs[j]]();
                exit();
            }
        }
    }

    // Wait for all child processes to terminate
     for (int i = 0; i < 6; ++i) {
        waitpid(pids[i]);
    }

    printf("Process table after all childs end:\n");
    taskManager.PrintProcessTable();

    printf("\nPART B the second strategy is successfully finished.\n");
}


void partB_third() {
    printf("PART B the preemptive strategy is running\n");   

    int pid;
    int pid2;
    int pid3;
    int pid4;

    int returnedPid = fork(&pid);

    if (returnedPid == pid) {
        // parent process
    }
    else {
        collatzSequence();
        exit();
    }

    returnedPid = fork(&pid2);
    if (returnedPid == pid2) {
    }
    else {
        longTask();
        exit();
    }

    returnedPid = fork(&pid3);
    if (returnedPid == pid3) {
    }
    else {
        binarySearch();
        exit();
    }

    returnedPid = fork(&pid4);
    if (returnedPid == pid4) {
    }
    else {
        linearSearch();
        exit();
    }

    waitpid(pid);
    waitpid(pid2);
    waitpid(pid3);
    waitpid(pid4);

    printf("Process table after all childs end:\n");
    taskManager.PrintProcessTable();

    printf("PART B preemptive priority strategy is successfully finished.\n");
}

 void initProcess() { 
    switch (interrupts.strategy) {
        case 0:
            partA_process();
            break;
        
        case 1:
            partB_first();
            break;

        case 2:
            partB_second();
            break;
        
        case 3:
            partB_third();
            break;
        
        case 4:
            partB_third();
            break;
        
        default:
            break;
    }     
 
}

typedef void (*constructor)();
extern "C" constructor start_ctors;
extern "C" constructor end_ctors;
extern "C" void callConstructors()
{
    for(constructor* i = &start_ctors; i != &end_ctors; i++)
        (*i)();
}

extern "C" void kernelMain(const void* multiboot_structure, uint32_t /*multiboot_magic*/)
{
    printf("Hello World!\n\n");
    uint32_t* memupper = (uint32_t*)(((size_t)multiboot_structure) + 8);
    size_t heap = 10*1024*1024;
    MemoryManager memoryManager(heap, (*memupper)*1024 - heap - 10*1024);
    void* allocated = memoryManager.malloc(1024);

    Task initTask(&gdt, initProcess);
    taskManager.InitTask(&initTask);

    interrupts.Activate();

    while(1); 
}
