    <script>-
        // Attendance Chart
        const attendanceCtx = document.getElementById('attendanceChart').getContext('2d');
        const attendanceChart = new Chart(attendanceCtx, {
            type: 'bar',
            data: {
                labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
                datasets: [
                    {
                        label: 'Present',
                        data: [65, 59, 80, 81, 56, 55, 40],
                        backgroundColor: 'rgba(67, 97, 238, 0.8)',
                        borderColor: 'rgba(67, 97, 238, 1)',
                        borderWidth: 0,
                        borderRadius: 8, // Rounded bar corners
                        borderSkipped: false,
                    },
                    {
                        label: 'Absent',
                        data: [5, 10, 3, 2, 7, 2, 1],
                        backgroundColor: 'rgba(239, 35, 60, 0.8)',
                        borderColor: 'rgba(239, 35, 60, 1)',
                        borderWidth: 0,
                        borderRadius: 8,
                        borderSkipped: false,
                    },
                    {
                        label: 'Late',
                        data: [12, 8, 5, 9, 4, 1, 0],
                        backgroundColor: 'rgba(248, 150, 30, 0.8)',
                        borderColor: 'rgba(248, 150, 30, 1)',
                        borderWidth: 0,
                        borderRadius: 8,
                        borderSkipped: false,
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            padding: 20
                        }
                    },
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        },
                        beginAtZero: true
                    }
                }
            }
        });

        // Department Distribution Chart
        const departmentCtx = document.getElementById('departmentChart').getContext('2d');
        const departmentChart = new Chart(departmentCtx, {
            type: 'doughnut',
            data: {
                labels: ['IT', 'HR', 'Finance', 'Marketing', 'Operations'],
                datasets: [{
                    data: [25, 15, 20, 15, 25],
                    backgroundColor: ['#4e73df', '#1cc88a', '#36b9cc', '#f6c23e', '#e74a3b'],
                    hoverBackgroundColor: ['#2e59d9', '#17a673', '#2c9faf', '#dda20a', '#be2617'],
                    hoverBorderColor: "rgba(234, 236, 244, 1)",
                }],
            },
            options: {
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'right',
                    },
                },
                cutout: '70%',
            },
        });
</script>

    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
