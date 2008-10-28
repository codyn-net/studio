require 'gtk2'

module Cpg
	class Simulation < GLib::Object
		type_register
	
		signal_new('step',
				GLib::Signal::RUN_LAST,
				nil,
				nil)

		signal_new('start',
				GLib::Signal::RUN_LAST,
				nil,
				nil)

		signal_new('stop',
				GLib::Signal::RUN_LAST,
				nil,
				nil)
		
		signal_new('period_start',
				GLib::Signal::RUN_LAST,
				nil,
				nil,
				Integer)

		signal_new('period_stop',
				GLib::Signal::RUN_LAST,
				nil,
				nil)
	
		attr_accessor :timestep
	
		def self.new(*args)
			if !@instance
				@instance = super
			end
		
			@instance
		end
		
		def self.instance
			@instance
		end

		def initialize(root, timestep)
			super({})

			self.root = root
		
			@timestep = timestep
			reset
		end
	
		def root=(r)
			@root = r || []
		end
		
		def period?
			@simulation_source === true
		end
		
		def simulate_period(*args)
			return false if running?
			
			@simulation_source = true
			
			if args.length == 1
				ts = @timestep
				from = 0
				to = args[0]
			elsif args.length == 2
				from, to = args[0..1]
				ts = @timestep
			elsif args.length > 2
				from, ts, to = args[0..2]
			end
			
			steps = ((to - from) / ts.to_f).to_i
			signal_emit('period_start', steps)
			
			objs = @root.select { |x| not x.is_a?(Components::Attachment) }
			@time = from
			
			(1..steps).each do |i|
				step(objs, ts)
			end
			
			signal_emit('period_stop')
			
			@simulation_source = nil
		end
	
		def step(objs = nil, dt = nil)
			states = {}
			dt ||= @timestep
			
			begin
				objs ||= @root.select { |x| not x.is_a?(Components::Attachment) }
				
				objs.each do |obj|
					states[obj] = obj.simulation_step(dt)
				end
	
				states.each do |obj,state|
					obj.simulation_update(state)
				end
			rescue Exception
				STDERR.puts("Error during simulation: #{$!}\n\t#{$@.join("\n\t")}")
			end

			@time += dt

			signal_emit('step')
		end
	
		def reset
			@time = 0
			@root.each { |obj| obj.simulation_reset }
		end
	
		def running?
			@simulation_source != nil
		end
	
		def state
			{:t => @time}
		end
	
		def stop
			return unless running?

			GLib::Source.remove(@simulation_source)
			@simulation_source = nil
			
			signal_emit('stop')
		end
	
		def start
			return if running?
			
			@simulation_source = GLib::Timeout.add(@timestep * 1000) { step; true}
			signal_emit('start')
		end
		
		def signal_do_step
		end
		
		def signal_do_start
		end
		
		def signal_do_stop
		end
		
		def signal_do_period_start(p)
		end
		
		def signal_do_period_stop
		end
	end
end
