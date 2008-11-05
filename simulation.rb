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
				Float,
				Float,
				Float)

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
			@monitors = {}
			reset
		end
	
		def root=(r)
			@root = r || []
		end
		
		def period?
			@simulation_source === true
		end
		
		def parse_period_range(*args)
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
			
			return from, ts, to
		end
		
		def resimulate
			simulate_period(*@range) if @range
		end
		
		def simulate_period(*args)
			return false if running?
			
			@simulation_source = true
			
			from, ts, to = parse_period_range(*args)
			@range = [from, ts, to]

			steps = ((to - from) / ts.to_f).to_i
			signal_emit('period_start', from, ts, to)
			
			objs = @root.select { |x| not x.is_a?(Components::Attachment) }
			@time = from
			
			(1..steps).each do |i|
				step(objs, ts)
			end
			
			signal_emit('period_stop')
			@range = [from, ts, to]
			@simulation_source = nil
		end
	
		def step(objs = nil, dt = nil)
			states = {}
			@range = nil
			dt ||= @timestep
			
			update_monitors
			
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
	
		def reset(resetobjs = true)
			@time = 0
			@root.each { |obj| obj.simulation_reset } if resetobjs
			
			@monitors.each do |monitor, properties| 
				properties.each { |property, values| values.each { |x| x.clear } }
			end
		end
		
		def setup_context(obj)
			if obj.is_a?(Components::Attachment)
				MathContext.new(self.state, obj.from ? obj.from.state : {}, obj.state, {:from => obj.from, :to => obj.to})
			else
				MathContext.new(self.state, obj.state)
			end
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
		
		# monitoring
		def monitors?(object, property)
			return @monitors[object] && @monitors[object][property.to_sym]
		end
		
		def set_monitor(object, property)
			property = property.to_sym

			@monitors[object] = {} unless @monitors[object]
			@monitors[object][property] = [[], []] unless @monitors[object][property]
		end
		
		def unset_monitor(object, property)
			@monitors[object].delete(property) if @monitors[object]
			
			if @monitors[object] && @monitors[object].empty?
				@monitors.delete(object)
			end
		end

		def update_monitors
			@monitors.each do |obj, properties|
				properties.each do |property, values|
					c = setup_context(obj)
				
					values[0] << c.eval(obj.get_property(property)).to_f
					values[1] << @time
				end
			end
		end
		
		def monitor_data(object, property)
			return [] unless @monitors[object]
			return @monitors[object][property][0] || []
		end
		
		def monitor_data_resampled(object, property, to)
			return [] unless @monitors[object]
			return [] unless @monitors[object][property]
			
			data = @monitors[object][property][0]
			sites = @monitors[object][property][1]
			
			data.resample(sites, to)
		end
		
		def signal_do_step
		end
		
		def signal_do_start
		end
		
		def signal_do_stop
		end
		
		def signal_do_period_start(from, timestep, to)
		end
		
		def signal_do_period_stop
		end
	end
end

begin
	require 'fastsimulation'
rescue Exception
	STDERR.puts("Unable to load fast simulation: #{$!}")
end
